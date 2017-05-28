namespace LastfmScrobbler.Models.Requests
{
    using System.Collections.Generic;
    using Resources;

    public abstract class BaseRequest
    {
        public string ApiKey => Strings.Keys.LastfmApiKey;

        public abstract string Method { get; }

        /// <summary>
        /// If the request is a secure request (Over HTTPS)
        /// </summary>
        public bool Secure { get; set; }

        public virtual Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "api_key", ApiKey },
                { "method",  Method }
            };
        }
    }

    public abstract class BaseAuthedRequest : BaseRequest
    {
        protected BaseAuthedRequest()
        {
            Secure = true;
        }

        public string SessionKey { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary()) 
            {
                { "sk", SessionKey },
            };
        }
    }

    public interface IPagedRequest
    {
        int Limit { get; set; }
        int Page  { get; set; }
    }
}
