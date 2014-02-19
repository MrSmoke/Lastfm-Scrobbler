namespace LastfmScrobbler.Models.Requests
{
    using System.Collections.Generic;

    public class BaseRequest
    {
        public string ApiKey { get; set; }
        public string Method { get; set; }

        /// <summary>
        /// If the request is a secure request (Over HTTPS)
        /// </summary>
        public bool Secure { get; set; }

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
