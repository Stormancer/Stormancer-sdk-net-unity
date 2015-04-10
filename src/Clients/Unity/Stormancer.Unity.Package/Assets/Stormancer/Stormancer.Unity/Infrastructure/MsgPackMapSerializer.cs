
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Client45.Infrastructure
{
    public class MsgPackMapSerializer : MsgPackSerializer
    {
        public MsgPackMapSerializer() : base() { }

        public MsgPackMapSerializer(IEnumerable<IMsgPackSerializationPlugin> plugins) : base(plugins) { }

        protected override MsgPack.Serialization.SerializationContext GetSerializationContext()
        {
            var ctx = base.GetSerializationContext();

            ctx.SerializationMethod = MsgPack.Serialization.SerializationMethod.Map;
            return ctx;
        }

        public override string Name
        {
            get { return "msgpack/map"; }
        }
    }
}
