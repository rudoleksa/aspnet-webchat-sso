// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ASP.NET.WebChat.SSO.Bot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace ASP.NET.WebChat.SSO.Bot.Dialogs
{
    public class LogoutDialog : ComponentDialog
    {
        protected readonly IStatePropertyAccessor<UserProfile> userProfileStateAccessor;

        public LogoutDialog(string id,
            IStatePropertyAccessor<UserProfile> userProfileStateAccessor)
            : base(id)
        {
            this.userProfileStateAccessor = userProfileStateAccessor;
        }

        protected string ConnectionName { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                var text = innerDc.Context.Activity.Text.Trim().ToLowerInvariant();

                // delete all stored data about user and sign out
                if (text == "logout")
                {
                    // The UserTokenClient encapsulates the authentication processes.
                    var userTokenClient = innerDc.Context.TurnState.Get<UserTokenClient>();
                    await userTokenClient.SignOutUserAsync(innerDc.Context.Activity.From.Id, ConnectionName, innerDc.Context.Activity.ChannelId, cancellationToken).ConfigureAwait(false);
                    await userProfileStateAccessor.DeleteAsync(innerDc.Context, cancellationToken);

                    await innerDc.Context.SendActivityAsync(MessageFactory.Text("You have been signed out."), cancellationToken);
                    return await innerDc.CancelAllDialogsAsync();
                }
            }

            return null;
        }
    }
}
