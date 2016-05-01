namespace LastfmScrobbler.Models.Responses
{
    using System.Runtime.Serialization;

    public class AuthenticationTokenResponse : BaseResponse
    {
        [DataMember(Name="token")]
        public string Token { get; set; }
    }
}