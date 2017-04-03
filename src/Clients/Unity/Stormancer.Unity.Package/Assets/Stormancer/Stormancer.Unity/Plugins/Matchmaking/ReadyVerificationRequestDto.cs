using MsgPack.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Stormancer.Plugins.Matchmaking
{
    public class ReadyVerificationRequestDto
    {
        [MessagePackMember(0)]
        public Dictionary<string, int> Members { get; set; }

        [MessagePackMember(1)]
        public string MatchId { get; set; }

        [MessagePackMember(2)]
        public int Timeout { get; set; }

        public ReadyVerificationRequest ToModel()
        {
            return new ReadyVerificationRequest
            {
                MatchId = this.MatchId,
                Timeout = this.Timeout,
                Members = this.Members.ToDictionary(kvp => kvp.Key, kvp => (Readiness)kvp.Value)
            };
        }
    }
}