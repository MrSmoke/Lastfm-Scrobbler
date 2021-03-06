﻿namespace LastfmScrobbler.Models.Requests
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Resources;

    [DataContract]
    public class NowPlayingRequest : BaseAuthedRequest
    {
        public string Track    { get; set; }
        public string Album    { get; set; }
        public string Artist   { get; set; }
        public int    Duration { get; set; }

        public override string Method => Strings.Methods.NowPlaying;

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary())
            {
                { "track",    Track  },
                { "album",    Album  },
                { "artist",   Artist },
                { "duration", Duration.ToString() }
            };
        }
    }
}
