using MsgPack.Serialization;

namespace Stormancer.Plugins.Friends.Dto
{
    public class FriendListUpdateDto
    {
        [MessagePackMember(0)]
        public string ItemId { get; set; }
        [MessagePackMember(1)]
        public string Operation { get; set; }
        [MessagePackMember(2)]
        public Friend Data { get; set; }
    }
}
