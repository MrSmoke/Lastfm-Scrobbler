namespace LastfmScrobbler.Models.Responses
{
    using System.Runtime.Serialization;

    [DataContract]
    public class GetUserInfoResponse : BaseResponse
    {
        public User User { get; set; }
    }

    [DataContract]
    public class User
    {
        
    }
}