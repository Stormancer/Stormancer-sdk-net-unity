using MsgPack.Serialization;

namespace Stormancer
{
    public class LeaderboardRanking
    {
        [MessagePackMember(0)]
        public int Ranking { get; set; }
        [MessagePackMember(1)]
        public ScoreRecord Document { get; set; }
    }
}