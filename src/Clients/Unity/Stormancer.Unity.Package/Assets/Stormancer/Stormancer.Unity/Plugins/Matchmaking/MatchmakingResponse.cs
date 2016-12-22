using MsgPack.Serialization;
using System.Collections.Generic;

namespace Stormancer.Plugins.Matchmaking
{
    public class MatchmakingResponse
    {
        [MessagePackMember(0)]
        public string GameId { get; set; }

        [MessagePackMember(1)]
        public List<Player> Team1 { get; set; }

        [MessagePackMember(2)]
        public List<Player> Team2 { get; set; }

        [MessagePackMember(3)]
        public List<string> OptionalParameters { get; set; }
    }
}