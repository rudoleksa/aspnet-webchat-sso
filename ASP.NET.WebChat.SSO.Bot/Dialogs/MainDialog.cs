// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ASP.NET.WebChat.SSO.Bot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ASP.NET.WebChat.SSO.Bot.Dialogs
{
    public class MainDialog : LogoutDialog
    {
        protected readonly ILogger _logger;

        public MainDialog(
            IStatePropertyAccessor<UserProfile> userProfileStateAccessor,
            IConfiguration configuration,
            ILogger<MainDialog> logger)
            : base(nameof(MainDialog), userProfileStateAccessor)
        {
            _logger = logger;

            ConnectionName = configuration["AadConnectionName"]; // OAuth AAD connection name created on Azure Bot Configuration

            // dialog for OAuth user authentication using AAD Identity Provider
            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = ConnectionName,
                    Text = "Please login",
                    Title = "Login",
                    Timeout = 300000, // User has 5 minutes to login
                }));

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                LoginStepAsync,
                CommandStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await userProfileStateAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            // if user is already signed in, skip auth step
            if (userProfile?.Token != null)
            {
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }

            // start auth dialog with OAuth Identity Provider Connection name selected by user
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tokenResponse = stepContext.Result as TokenResponse;

            var userProfile = await userProfileStateAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            // if user is already signed in, user didn`t run auth dialog. Therefore we take the stored token
            var token = tokenResponse?.Token ?? userProfile?.Token;

            // if no stored token and no response from auth dialog - auth error
            if (token == null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful please try again."), cancellationToken);
                return await stepContext.EndDialogAsync();
            }

            var user = await OAuthHelpers.GetUserAsync(stepContext.Context, token);
            userProfile.Name = user.DisplayName;
            userProfile.Token = token;

            await userProfileStateAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);

            var choices = ChoiceFactory.ToChoices(new List<string> { "Token" });

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions()
            {
                Prompt = MessageFactory.Text($"Hello, {userProfile.Name}"),
                Choices = choices,
                Style = ListStyle.HeroCard,
            },
            cancellationToken);
        }

        private async Task<DialogTurnResult> CommandStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var response = (FoundChoice)stepContext.Result;

            // if selected button to show token, get stored token
            if (response.Index == 0)
            {
                var userProfile = await userProfileStateAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Your token is: {userProfile.Token}"), cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
