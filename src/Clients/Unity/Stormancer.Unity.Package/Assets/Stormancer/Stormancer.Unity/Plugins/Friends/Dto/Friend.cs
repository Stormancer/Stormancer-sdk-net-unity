using MsgPack.Serialization;
using System;

namespace Stormancer.Plugins.Friends.Dto
{
    public class Friend
    {
        [MessagePackMember(0)]
        public string UserId { get; set; }

        [MessagePackMember(1)]
        public string Details { get; set; }

        [MessagePackMember(2)]
        public DateTime LastConnected { get; set; }

        [MessagePackMember(3)]
        public FriendStatus Status { get; set; }
    }
}
