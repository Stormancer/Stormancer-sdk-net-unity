using MsgPack.Serialization;
using System.Collections.Generic;

namespace Stormancer
{
    public class LeaderboardQuery
    {
        public LeaderboardQuery()
        {
            FriendsIds = new List<string>();
        }

        [MessagePackMember(0)]
        public string StartId { get; set; }

        [MessagePackMember(1)]
        public List<ScoreFilter> ScoreFilters { get; set; }

        [MessagePackMember(2)]
        public List<FieldFilter> FieldFilters { get; set; }

        [MessagePackMember(3)]
        public int Count { get; set; }

        [MessagePackMember(4)]
        public int Skip { get; set; }

        [MessagePackMember(5)]
        public string Name { get; set; }

        [MessagePackMember(6)]
        public List<string> FriendsIds { get; set; }
    }
}