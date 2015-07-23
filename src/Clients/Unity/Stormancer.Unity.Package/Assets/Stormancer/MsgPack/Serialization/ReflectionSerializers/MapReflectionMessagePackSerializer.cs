#if UNITY_IOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace MsgPack.Serialization.ReflectionSerializers
{
    internal class MapReflectionMessagePackSerializer : MessagePackSerializer
    {
        private readonly Func<object, int> _getCount;
        private readonly CollectionTraits _traits;
        private readonly IMessagePackSerializer _keySerializer;
        private readonly IMessagePackSerializer _valueSerializer;
        private readonly Action<Packer, object, IMessagePackSerializer, IMessagePackSerializer> _packToCore;
        private readonly Action<Unpacker, object, IMessagePackSerializer, IMessagePackSerializer> _unpackToCore;
        private readonly Func<int, object> _createInstanceWithCapacity;
        private readonly Func<object> _createInstance;

        public MapReflectionMessagePackSerializer(Type type, SerializationContext context, CollectionTraits traits)
            : base(type, (context ?? SerializationContext.Default).CompatibilityOptions.PackerCompatibilityOptions)
        {
            Contract.Assert(typeof(IEnumerable).IsAssignableFrom(type), type + " is IEnumerable");
            Contract.Assert(traits.ElementType == typeof(DictionaryEntry) || (traits.ElementType.GetIsGenericType() && traits.ElementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)), "Element type " + traits.ElementType + " is not KeyValuePair<TKey,TValue>.");
            this._traits = traits;
            this._keySerializer = traits.ElementType.GetIsGenericType() ? context.GetSerializer(traits.ElementType.GetGenericArguments()[0]) : context.GetSerializer(typeof(MessagePackObject));
            this._valueSerializer = traits.ElementType.GetIsGenericType() ? context.GetSerializer(traits.ElementType.GetGenericArguments()[1]) : context.GetSerializer(typeof(MessagePackObject));
            this._getCount = ReflectionSerializerLogics.CreateGetCount(type, traits);

            var constructor = ReflectionSerializerLogics.GetCollectionConstructor(context, type);

            if (constructor == null)
            {
                this._createInstance = () => { throw SerializationExceptions.NewTargetDoesNotHavePublicDefaultConstructorNorInitialCapacity(type); };
                this._createInstanceWithCapacity = null;
            }
            else if (constructor.GetParameters().Length == 1)
            {
                this._createInstance = null;

                this._createInstanceWithCapacity = length => constructor.Invoke(new object[] { length });
            }
            else
            {
                this._createInstanceWithCapacity = null;
                this._createInstance = () => constructor.Invoke(new object[0]);
            }

            var keyType = traits.ElementType.GetIsGenericType() ? traits.ElementType.GetGenericArguments()[0] : typeof(MessagePackObject);
            var valueType = traits.ElementType.GetIsGenericType() ? traits.ElementType.GetGenericArguments()[1] : typeof(MessagePackObject);
            var keyProperty = traits.ElementType.GetProperty("Key");
            var valueProperty = traits.ElementType.GetProperty("Value");

            this._packToCore = (Packer packer, object objectTree, IMessagePackSerializer keySerializer, IMessagePackSerializer valueSerializer) =>
                {
                    packer.PackMapHeader(this._getCount(objectTree));
                    foreach (var kvp in (IEnumerable)objectTree)
                    {
                        keySerializer.PackTo(packer, keyProperty.GetValue(kvp, new object[0]));
                        valueSerializer.PackTo(packer, valueProperty.GetValue(kvp, new object[0]));
                    }
                };

            if (traits.ElementType.GetIsGenericType())
            {
                /*
                 * UnpackHelpers.UnpackMapTo<TKey,TValue>( unpacker, keySerializer, valueSerializer, instance );
                 */
                var unpackMapToMethod =   Metadata._UnpackHelpers.UnpackMapTo_2; //.MakeGenericMethod(keyType, valueType);
                this._unpackToCore = (Unpacker unpacker, object objectTree, IMessagePackSerializer keySerializer, IMessagePackSerializer valueSerializer) =>
                    {
                        
                        unpackMapToMethod.Invoke(null, new object[] { unpacker, keySerializer, valueSerializer, objectTree });
                    };
            }
            else
            {
                /*
                 * UnpackHelpers.UnpackNonGenericMapTo( unpacker, instance );
                 */
                this._unpackToCore = (Unpacker unpacker, object objectTree, IMessagePackSerializer keySerializer, IMessagePackSerializer valueSerializer) => UnpackHelpers.UnpackMapTo(unpacker, (IDictionary)objectTree);
            }
        }

        protected internal override void PackToCore(Packer packer, object objectTree)
        {
            this._packToCore(packer, objectTree, this._keySerializer, this._valueSerializer);
        }

        protected internal override object UnpackFromCore(Unpacker unpacker)
        {
            var instance = this._createInstanceWithCapacity == null ? this._createInstance() : this._createInstanceWithCapacity(UnpackHelpers.GetItemsCount(unpacker));
            this.UnpackTo(unpacker, instance);
            return instance;
        }

        protected internal override void UnpackToCore(Unpacker unpacker, object collection)
        {
            this._unpackToCore(unpacker, collection, this._keySerializer, this._valueSerializer);
        }

    }
}
#endif