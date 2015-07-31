#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgPack.Serialization.ReflectionSerializers
{
    internal class MapFormatObjectReflectionMessagePackSerializer : ObjectReflectionMessagePackSerializer
    {
        public MapFormatObjectReflectionMessagePackSerializer(Type type, SerializationContext context, SerializingMember[] members ) : base(type, context, members ) { }

        protected override void PackToCoreOverride(Packer packer, object objectTree)
        {
            packer.PackMapHeader(this.MemberSerializers.Length);

            for (int i = 0; i < this.MemberSerializers.Length; i++)
            {
                if (this.MemberNames[i] == null)
                {
                    // Skip missing member.
                    continue;
                }

                packer.PackString(this.MemberNames[i]);
                this.MemberSerializers[i].PackTo(packer, this.MemberGetters[i](objectTree));
            }
        }
    }
}
#endif