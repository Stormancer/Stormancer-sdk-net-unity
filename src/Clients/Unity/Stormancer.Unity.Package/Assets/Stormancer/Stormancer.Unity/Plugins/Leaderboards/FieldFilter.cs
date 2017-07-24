using MsgPack.Serialization;

namespace Stormancer
{
    public class FieldFilter
    {
        [MessagePackMember(0)]
        public string Field { get; set; }
        [MessagePackMember(1)]
        public string Value { get; set; }
    }
}