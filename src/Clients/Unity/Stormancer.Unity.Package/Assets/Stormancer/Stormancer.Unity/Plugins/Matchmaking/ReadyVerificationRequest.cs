using System.Collections.Generic;
using System.Linq;

namespace Stormancer.Plugins.Matchmaking
{
    public class ReadyVerificationRequest
    {
        public Dictionary<string, Readiness> Members { get; set; }
        public string MatchId { get; set; }
        public int Timeout { get; set; }
    }
}