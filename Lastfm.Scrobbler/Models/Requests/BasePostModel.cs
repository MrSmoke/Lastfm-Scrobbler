namespace Lastfm.Scrobbler.Models.Requests
{
    using System.Collections.Generic;

    public class BaseRequest
    {
        public string ApiKey { get; set; }
        public string Method { get; set; }

        public virtual Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>()
            {
                { "api_key", this.ApiKey },
                { "method", this.Method }
            };
        }
    }

    public class BaseAuthedRequest : BaseRequest
    {
        public string SessionKey { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary()) 
            {
                { "sk", this.SessionKey },
            };
        }
    }
}
