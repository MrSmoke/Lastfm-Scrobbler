namespace LastfmScrobbler.Models.Requests
{
    using System.Collections.Generic;

    public class SessionRequest : BaseRequest
    {
        public string Token { get; set; }

        public override Dictionary<string, string> ToDictionary() 
        {
            return new Dictionary<string, string>(base.ToDictionary()) 
            {
                { "token", Token }
            };
        }
    }
}
