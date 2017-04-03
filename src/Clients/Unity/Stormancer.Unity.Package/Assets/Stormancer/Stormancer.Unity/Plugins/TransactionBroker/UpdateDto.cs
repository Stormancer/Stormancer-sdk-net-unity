using MsgPack.Serialization;
using System;

namespace Stormancer.Plugins.TransactionBroker
{
    public class UpdateDto
    {
        [MessagePackMember(0)]
        public int FinalStepId { get; set; }

        [MessagePackMember(1)]
        public DateTime CreatedOn { get; set; }

        [MessagePackMember(2)]
        public string IssuerUserId { get; set; }

        [MessagePackMember(3)]
        public string IssuerPlayerId { get; set; }

        [MessagePackMember(4)]
        public string Command { get; set; }

        [MessagePackMember(5)]
        public string Arguments { get; set; }
    }
}