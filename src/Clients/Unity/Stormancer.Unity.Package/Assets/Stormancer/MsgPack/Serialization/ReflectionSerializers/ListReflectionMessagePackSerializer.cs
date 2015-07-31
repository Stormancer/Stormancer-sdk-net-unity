#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgPack.Serialization.ReflectionSerializers
{
    internal class ListReflectionMessagePackSerializer : SequenceReflectionMessagePackSerializer
    {
        private readonly Func<int, object> _createInstanceWithCapacity;
        private readonly Func<object> _createInstance;

        public ListReflectionMessagePackSerializer(Type type, SerializationContext context, CollectionTraits traits)
            : base(type, context, traits)
        {
            if (type.GetIsAbstract())
            {
                type = context.DefaultCollectionTypes.GetConcreteType(type) ?? type;
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                this._createInstanceWithCapacity = length =>
                    {
                        return Array.CreateInstance(elementType, length);
                    };
                this._createInstance = null;
            }
            else if (type.GetIsAbstract())
            {
                this._createInstance = () => { throw SerializationExceptions.NewNotSupportedBecauseCannotInstanciateAbstractType(type); };
                this._createInstanceWithCapacity = null;
            }
            else
            {
                var constructor = ReflectionSerializerLogics.GetCollectionConstructor(context, type);
                if (constructor == null)
                {
                    this._createInstance = () => { throw SerializationExceptions.NewTargetDoesNotHavePublicDefaultConstructorNorInitialCapacity(type); };
                    this._createInstanceWithCapacity = null;
                }
                else
                {
                    if (constructor.GetParameters().Length == 1)
                    {
                        this._createInstance = null;

                        this._createInstanceWithCapacity = length => constructor.Invoke(new object[] { length });
                    }
                    else
                    {
                        this._createInstanceWithCapacity = null;
                        this._createInstance = () => constructor.Invoke(new object[0]);
                    }
                }
            }
        }

        protected internal override object UnpackFromCore(Unpacker unpacker)
        {
            if (!unpacker.IsArrayHeader)
            {
                throw SerializationExceptions.NewIsNotArrayHeader();
            }

            var instance = this._createInstanceWithCapacity == null ? this._createInstance() : this._createInstanceWithCapacity(UnpackHelpers.GetItemsCount(unpacker));
            this.UnpackToCore(unpacker, instance);
            return instance;
        }
    }
}
#endif