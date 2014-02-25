namespace LastfmScrobbler.Models.Requests
{
    using System;
    using System.Collections.Generic;

    public class GetTracksRequest : BaseRequest, IPagedRequest
    {
        public string User   { get; set; }
        public string Artist { get; set; }
        public int    Limit  { get; set; }
        public int    Page   { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary()) 
            {
                { "user",   this.User   },
                { "artist", this.Artist },
                { "limit" , this.Limit.ToString() },
                { "page"  , this.Page.ToString()  }
            };
        }
    }
}
