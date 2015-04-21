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
        
        /// <summary>
        /// Creates a new MsgPackSerializer object
        /// </summary>
        public MsgPackSerializer() : this(null) { }

        /// <summary>
        /// Creates a new MsgPackSerializer object with plugins
        /// </summary>
        /// <param name="plugins">A collection of serialization plugins</param>
        public MsgPackSerializer(IEnumerable<IMsgPackSerializationPlugin> plugins)
        {
            if (plugins == null)
            {
                plugins = Enumerable.Empty<IMsgPackSerializationPlugin>();
            }

            this._plugins = plugins;
        }

        /// <summary>
        /// Serializes an object into a stream
        /// </summary>
        /// <typeparam name="T">The Type of the object to deserialize</typeparam>
        /// <param name="data">An object to serialize</param>
        /// <param name="stream">A Stream into which the object will be serialized</param>
        /// <remarks>
        /// The method doesn't close the stream
        /// </remarks>
        public void Serialize<T>(T data, System.IO.Stream stream)
        {
            var serializer = (MsgPack.Serialization.MessagePackSerializer<T>)_serializersCache.GetOrAdd(typeof(T),k=> MsgPack.Serialization.MessagePackSerializer.Get<T>(GetSerializationContext()));


            serializer.PackTo(Packer.Create(stream, false), data);
        }

        /// <summary>
        /// Deserialize an instance of T from a stream.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into</typeparam>
        /// <param name="stream">A binary stream the object will be deserialized from</param>
        /// <remarks>
        /// The method don't close the stream
        /// </remarks>
        /// <returns>An instance of T deserialized from the stream</returns>
        public T Deserialize<T>(System.IO.Stream stream)
        {

            var serializer = (MsgPack.Serialization.MessagePackSerializer<T>)_serializersCache.GetOrAdd(typeof(T),k=> MsgPack.Serialization.MessagePackSerializer.Get<T>(GetSerializationContext()));

            var unpacker = Unpacker.Create(stream, false);
            unpacker.Read();
            return serializer.UnpackFrom(unpacker);
        }

        /// <summary>
        /// Builds the msgpack serialization context for this serializer.
        /// </summary>
        /// <returns>
        /// The new serialization context.
        /// </returns>
        protected virtual SerializationContext GetSerializationContext()
        {
            var ctx = new MsgPack.Serialization.SerializationContext();
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
            foreach (var plugin in _plugins)
            {
                plugin.OnCreatingSerializationContext(ctx);
            }
 
            return ctx;
        }

        /// <summary>
        /// Name of the serializer
        /// </summary>
        /// <remarks>
        /// Returns 'msgpack/array'
        /// </remarks>
        public virtual string Name
        {
            get { return "msgpack/array"; }
        }


    }

    /// <summary>
    /// A custom msgPack serializer that allows to declare its serialization logic using lambda methods
    /// </summary>
    /// <typeparam name="T">The type that this serializer will serialize/deserialize</typeparam>
    public sealed class MsgPackLambdaTypeSerializer<T> : MessagePackSerializer<T>
    {
        private readonly Action<MsgPack.Packer, T> _pack;
        private readonly Func<MsgPack.Unpacker, T> _unpack;

        /// <summary>
        /// Creates a MsgPackLambdaTypeSerializer instance
        /// </summary>
        /// <param name="pack">An action that is executed when an instance of T has to be serialized</param>
        /// <param name="unpack">A function that is executed when an instance of T has to be deserialized</param>
        /// <param name="ctx">The serialization context</param>
        public MsgPackLambdaTypeSerializer(Action<MsgPack.Packer, T> pack, Func<MsgPack.Unpacker, T> unpack, SerializationContext ctx)
            : base(ctx)
        {
            _pack = pack;
            _unpack = unpack;
        }

        /// <summary>
        /// Serializes the target object
        /// </summary>
        /// <param name="packer"></param>
        /// <param name="objectTree"></param>
        protected override void PackToCore(MsgPack.Packer packer, T objectTree)
        {
            _pack(packer, objectTree);
        }

        /// <summary>
        /// Deserializes the target object
        /// </summary>
        /// <param name="unpacker"></param>
        /// <returns></returns>
        protected override T UnpackFromCore(MsgPack.Unpacker unpacker)
        {
            return _unpack(unpacker);
        }
    }

    /// <summary>
    /// Declares a plugin that can customize the msgpack serialization process
    /// </summary>
    public interface IMsgPackSerializationPlugin
    {
        /// <summary>
        /// Registers custom serializers into the msgpack serializer
        /// </summary>
        /// <param name="ctx">A SerializationContext instance that allows the plugin to register custom serialization logic</param>
        void OnCreatingSerializationContext(SerializationContext ctx);
    }


}
