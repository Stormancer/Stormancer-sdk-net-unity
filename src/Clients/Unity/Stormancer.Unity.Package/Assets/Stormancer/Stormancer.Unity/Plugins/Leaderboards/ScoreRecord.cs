using MsgPack.Serialization;
using System;

namespace Stormancer
{
    public class ScoreRecord
    {
        [MessagePackMember(0)]
        public string Id { get; set; }

        [MessagePackMember(1)]
        public int Score { get; set; }

        [MessagePackMember(2)]
        public DateTime CreatedOn { get; set; }

        [MessagePackMember(3)]
        public string Document { get; set; }
    }
}