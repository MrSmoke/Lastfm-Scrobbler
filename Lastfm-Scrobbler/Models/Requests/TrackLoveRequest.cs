namespace LastfmScrobbler.Models.Requests
{
    using System.Collections.Generic;
    using Resources;

    public class TrackLoveRequest : BaseAuthedRequest
    {
        private readonly bool _love;

        public TrackLoveRequest(bool love)
        {
            _love = love;
        }

        public string Track  { get; set; }
        public string Artist { get; set; }

        public override string Method => _love ? Strings.Methods.TrackLove : Strings.Methods.TrackUnlove;

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary())
            {
                { "track" , Track  },
                { "artist", Artist }
            };
        }
    }
}
