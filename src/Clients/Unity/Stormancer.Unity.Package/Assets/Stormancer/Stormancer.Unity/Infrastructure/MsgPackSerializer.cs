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


        protected virtual SerializationContext GetSerializationContext()
        {
            var ctx = new MsgPack.Serialization.SerializationContext();

            foreach (var plugin in _plugins)
            {
                plugin.OnCreatingSerializationContext(ctx);
            }
            return ctx;
        }

        public virtual string Name
        {
            get { return "msgpack/array"; }
        }


    }

    public class MsgPackLambdaTypeSerializer<T> 
#if UNITY_IOS
        :MessagePackSerializer
#else
        : MessagePackSerializer<T>
#endif
        {
        private readonly Action<MsgPack.Packer, T> _pack;
        private readonly Func<MsgPack.Unpacker, T> _unpack;
        public MsgPackLambdaTypeSerializer(Action<MsgPack.Packer, T> pack, Func<MsgPack.Unpacker, T> unpack, SerializationContext ctx)
#if UNITY_IOS
            :base(typeof(T), ctx.CompatibilityOptions.PackerCompatibilityOptions)
#else
            : base(ctx.CompatibilityOptions.PackerCompatibilityOptions)
#endif
            {
            _pack = pack;
            _unpack = unpack;
        }
#if UNITY_IOS
        protected internal override void PackToCore(Packer packer, object objectTree)
#else
        protected internal override void PackToCore(MsgPack.Packer packer, T objectTree)
#endif
        {
            _pack(packer, (T)objectTree);
        }
#if UNITY_IOS
        protected internal override object UnpackFromCore(MsgPack.Unpacker unpacker)
#else
        protected internal override T UnpackFromCore(MsgPack.Unpacker unpacker)
#endif
        {
            return _unpack(unpacker);
        }
    }

    public interface IMsgPackSerializationPlugin
    {
        void OnCreatingSerializationContext(SerializationContext ctx);
    }


}
