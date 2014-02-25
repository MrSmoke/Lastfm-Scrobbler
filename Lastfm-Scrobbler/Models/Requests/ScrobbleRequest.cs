namespace LastfmScrobbler.Models.Requests
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class ScrobbleRequest : BaseAuthedRequest
    {
        [DataMember(Name="track")]
        public string Track { get; set; }

        [DataMember(Name="artist")]
        public string Artist { get; set; }

        [DataMember(Name="timestamp")]
        public int Timestamp { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary()) 
            {
                { "track",     Track },
                { "artist",    Artist },
                { "timestamp", Timestamp.ToString() }
            };
        }
    }
}
