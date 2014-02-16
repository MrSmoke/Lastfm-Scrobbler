using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LastfmScrobbler.Models.Responses
{
    [DataContract]
    public class MobileSessionResponse : BaseResponse
    {
        [DataMember(Name="session")]
        public MobileSession Session { get; set; }
    }
}
