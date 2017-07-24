using MsgPack.Serialization;

namespace Stormancer
{
    public class ScoreFilter
    {
        [MessagePackMember(0)]
        public ScoreFilterType Type { get; set; }

        [MessagePackMember(1)]
        public long Value { get; set; }
    }
}