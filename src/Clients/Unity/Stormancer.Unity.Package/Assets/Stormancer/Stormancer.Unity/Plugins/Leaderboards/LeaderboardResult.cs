using MsgPack.Serialization;
using System.Collections.Generic;

namespace Stormancer
{
    public class LeaderboardResult
    {
        public LeaderboardResult()
        {
            Results = new List<LeaderboardRanking>();
        }

        [MessagePackMember(0)]
        public List<LeaderboardRanking> Results { get; set; } 

        [MessagePackMember(1)]
        public string Next { get; set; }

        [MessagePackMember(2)]
        public string Previous { get; set; }

        [MessagePackMember(3)]
        public int Hits { get; set; }
    }
}