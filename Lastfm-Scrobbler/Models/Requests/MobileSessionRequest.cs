namespace LastfmScrobbler.Models.Requests
{
    using System.Collections.Generic;
    using Resources;

    public class MobileSessionRequest : BaseRequest
    {
        public string Password { get; set; }
        public string Username { get; set; }

        public override string Method => Strings.Methods.GetMobileSession;

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary())
            {
                { "password", Password },
                { "username", Username },
            };
        }
    }
}
