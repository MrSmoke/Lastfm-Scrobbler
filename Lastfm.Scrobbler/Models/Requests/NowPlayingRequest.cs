namespace Lastfm.Scrobbler.Models.Requests
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class NowPlayingRequest : BaseAuthedRequest
    {
        [DataMember(Name = "track")]
        public string Track { get; set; }

        [DataMember(Name = "artist")]
        public string Artist { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary()) 
            {   
                { "track",    this.Track         },
                { "artist",   this.Artist        },              
            };
        }
    }
}
