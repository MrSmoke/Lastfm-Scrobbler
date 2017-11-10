namespace LastfmScrobbler.Models.Responses
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SessionResponse : BaseResponse
    {
        [DataMember(Name="session")]
        public Session Session { get; set; }
    }
}
