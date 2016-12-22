using MsgPack.Serialization;

namespace Stormancer.Plugins.GameSession
{
    public class PlayerUpdate
    {
        [MessagePackMember(0)]
        public string UserId { get; set; }

        [MessagePackMember(1)]
        public int Status { get; set; }

        [MessagePackMember(2)]
        public string FaultReason { get; set; }
    }
}