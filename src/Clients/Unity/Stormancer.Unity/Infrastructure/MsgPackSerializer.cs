using MsgPack;
using MsgPack.Serialization;
using System;
using System.Collections.Concurrent;
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
        private readonly IEnumerable<IMsgPackSerializationPlugin> _plugins;

        private ConcurrentDictionary<Type, object> _serializersCache = new ConcurrentDictionary<Type, object>();


        public MsgPackSerializer() : this(null) { }
        public MsgPackSerializer(IEnumerable<IMsgPackSerializationPlugin> plugins)
        {
            if (plugins == null)
            {
                plugins = Enumerable.Empty<IMsgPackSerializationPlugin>();
            }

            this._plugins = plugins;
        }
        public void Serialize<T>(T data, System.IO.Stream stream)
        {
            var serializer = (MsgPack.Serialization.MessagePackSerializer<T>)_serializersCache.GetOrAdd(typeof(T), k => MsgPack.Serialization.MessagePackSerializer.Create<T>(GetSerializationContext()));


            serializer.PackTo(Packer.Create(stream, false), data);
        }

        public T Deserialize<T>(System.IO.Stream stream)
        {

            var serializer = (MsgPack.Serialization.MessagePackSerializer<T>)_serializersCache.GetOrAdd(typeof(T), k => MsgPack.Serialization.MessagePackSerializer.Create<T>(GetSerializationContext()));

            var unpacker = Unpacker.Create(stream, false);
            unpacker.Read();
            return serializer.UnpackFrom(unpacker);
        }


        private SerializationContext GetSerializationContext()
        {
            var ctx = new MsgPack.Serialization.SerializationContext();

#if UNITY_4_6
#else
            var jobjectSerializer = new MsgPackLambdaTypeSerializer<Newtonsoft.Json.Linq.JObject>(
                (p, o) =>
                {

                    p.PackString(o.ToString());
                },
                p =>
                {
                    var json = p.LastReadData.AsString();
                    return Newtonsoft.Json.Linq.JObject.Parse(json);
                },
                ctx
                );
            ctx.Serializers.Register(jobjectSerializer);
#endif

            foreach (var plugin in _plugins)
            {
                plugin.OnCreatingSerializationContext(ctx);
            }
            return ctx;
        }

        public string Name
        {
            get { return "msgpack/array"; }
        }


    }

    public class MsgPackLambdaTypeSerializer<T> : MessagePackSerializer<T>
    {
        private readonly Action<MsgPack.Packer, T> _pack;
        private readonly Func<MsgPack.Unpacker, T> _unpack;
        public MsgPackLambdaTypeSerializer(Action<MsgPack.Packer, T> pack, Func<MsgPack.Unpacker, T> unpack, SerializationContext ctx)
            : base(ctx.CompatibilityOptions.PackerCompatibilityOptions)
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

    public interface IMsgPackSerializationPlugin
    {
        void OnCreatingSerializationContext(SerializationContext ctx);
    }


}
