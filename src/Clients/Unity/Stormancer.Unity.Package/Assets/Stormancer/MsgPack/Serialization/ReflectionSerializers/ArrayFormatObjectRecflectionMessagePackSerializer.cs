#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgPack.Serialization.ReflectionSerializers
{
    internal class ArrayFormatObjectRecflectionMessagePackSerializer : ObjectReflectionMessagePackSerializer
    {
        public ArrayFormatObjectRecflectionMessagePackSerializer(Type type, SerializationContext context, SerializingMember[] members) : base(type, context, members) { }

        protected override void PackToCoreOverride(Packer packer, object objectTree)
        {
            packer.PackArrayHeader(this.MemberSerializers.Length);

            for (int i = 0; i < this.MemberSerializers.Length; i++)
            {
                this.MemberSerializers[i].PackTo(packer, this.MemberGetters[i](objectTree));
            }
        }
    }
}

#endif