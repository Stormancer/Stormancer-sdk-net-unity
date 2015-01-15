using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Client45.Infrastructure
{
    /// <summary>
    /// Serializer based on MsgPack.
    /// </summary>
    public class MsgPackSerializer : ISerializer
    {
        public void Serialize<T>(T data, System.IO.Stream stream)
        {
            var serializer = MsgPack.Serialization.MessagePackSerializer.Create<T>(GetSerializationContext());

            serializer.Pack(stream, data);
        }

        public T Deserialize<T>(System.IO.Stream stream)
        {

            var serializer = MsgPack.Serialization.MessagePackSerializer.Create<T>(GetSerializationContext());

            return serializer.Unpack(stream);
        }

        private SerializationContext GetSerializationContext()
        {
            var ctx = new MsgPack.Serialization.SerializationContext();
            //ctx.Serializers.Register(new TypeSerializer<Newtonsoft.Json.Linq.JObject>(
            //    (p, o) => 
            //    {
                    
            //        p.PackString(o.ToString());
            //    },
            //    p =>
            //    {
            //        var json = p.LastReadData.AsString();
            //        return Newtonsoft.Json.Linq.JObject.Parse(json);
            //    }
            //    ));

            return ctx;
        }

        public string Name
        {
            get { return "msgpack/array"; }
        }

        internal class TypeSerializer<T> : MessagePackSerializer<T>
        {
            private readonly Action<MsgPack.Packer, T> _pack;
            private readonly Func<MsgPack.Unpacker, T> _unpack;
            public TypeSerializer(Action<MsgPack.Packer, T> pack, Func<MsgPack.Unpacker, T> unpack)
            {
                _pack = pack;
                _unpack = unpack;
            }
            protected override void PackToCore(MsgPack.Packer packer, T objectTree)
            {
                _pack(packer, objectTree);
            }

            protected override T UnpackFromCore(MsgPack.Unpacker unpacker)
            {
                return _unpack(unpacker);
            }
        }
    }


}
