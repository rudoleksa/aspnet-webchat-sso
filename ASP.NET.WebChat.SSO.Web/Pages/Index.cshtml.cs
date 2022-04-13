using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ASP.NET.WebChat.SSO.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ITokenAcquisition tokenAcquisition;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ITokenAcquisition tokenAcquisition, ILogger<IndexModel> logger)
        {
            this.tokenAcquisition = tokenAcquisition;
            _logger = logger;
        }

        [BindProperty]
        public string Token { get; set; }
        [BindProperty]
        public string UserId { get; set; }
        [BindProperty]
        public string UserToken { get; set; }

        public async Task OnGetAsync()
        {
            var secret = "<WebChat Secret>";
            var resourceUri = "<Resource Uri>";
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(
                      HttpMethod.Post,
                      $" https://directline.botframework.com/v3/directline/tokens/generate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);

            var id = User?.Claims?.FirstOrDefault(u => u.Type == ClaimConstants.ObjectId)?.Value;
            var userId = $"dl_{id ?? Guid.NewGuid().ToString()}";
            request.Content = new StringContent(
            JsonConvert.SerializeObject(
                new { User = new { Id = userId } }),
                Encoding.UTF8,
                "application/json");
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            string token = string.Empty;
            if (response.IsSuccessStatusCode)
            {
                token = JsonConvert.DeserializeObject<DirectLineToken>(body).token;
            }

            Token = token;
            UserId = userId;

            try
            {
                UserToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { resourceUri });
            }
            catch
            {

            }
        }

        public async Task<IActionResult> OnGetOAuthAsync(string conversationId, string userId, string token, string connectionName, string resourceUri)
        {
            var directLineActivityUri = $"https://directline.botframework.com/v3/directline/conversations/{conversationId}/activities";

            try
            {
                using var httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var userToken = await tokenAcquisition.GetAccessTokenForUserAsync(new[] { resourceUri });
                
                var body = new Activity
                {
                    Type = "Invoke",
                    Name = "signin/tokenExchange",
                    Value = new
                    {
                        id = Guid.NewGuid().ToString(),
                        connectionName = connectionName,
                        token = userToken
                    }
                };

                var request = JsonConvert.SerializeObject(body);

                var response = await httpClient.PostAsJsonAsync(directLineActivityUri, body);

                var json = await response.Content.ReadAsStringAsync();

            }
            catch
            {

            }

            return new JsonResult("test");
        }

        public class DirectLineToken
        {
            public string conversationId { get; set; }
            public string token { get; set; }
            public int expires_in { get; set; }
        }

        public class OAuthActivity
        {
            public OAuthActivity(OAuthValue value)
            {
                this.value = value;
                type = "Invoke";
                name = "signin/tokenExchange";
            }

            public string? type { get; set; }
            public string? name { get; set; }
            public OAuthValue? value { get; set; }
        }

        public class OAuthValue
        {
            public string? id { get; set; }
            public string? connectionName { get; set; }
            public string? token { get; set; }

        }
    }
}