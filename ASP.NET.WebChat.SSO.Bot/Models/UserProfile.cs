using System;

namespace ASP.NET.WebChat.SSO.Bot.Models
{
    public class UserProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Token { get; set; }


        public static readonly Func<UserProfile> Factory = () => new UserProfile();
    }
}
