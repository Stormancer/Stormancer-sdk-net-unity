using MsgPack.Serialization;

namespace Stormancer
{
    public class GameServerInformation
    {
        [MessagePackMember(0)]
        public string Ip { get; set; }

        [MessagePackMember(1)]
        public int Port { get; set; }
    }
}