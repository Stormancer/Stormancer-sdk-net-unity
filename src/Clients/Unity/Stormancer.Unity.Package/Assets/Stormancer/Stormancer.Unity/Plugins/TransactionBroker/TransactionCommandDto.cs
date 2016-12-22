using MsgPack.Serialization;

namespace Stormancer.Plugins.TransactionBroker
{
    public  class TransactionCommandDto
    {
        [MessagePackMember(0)]
        public string PlayerId { get; set; }

        [MessagePackMember(1)]
        public string Command { get; set; }

        [MessagePackMember(2)]
        public string Args { get; set; }
    }
}