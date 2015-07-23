#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgPack.Serialization.ReflectionSerializers
{
    class ArrayRecflectionMessagePackSerializer : SequenceReflectionMessagePackSerializer
    {
        public ArrayRecflectionMessagePackSerializer(Type type, SerializationContext context, CollectionTraits traits) : base(type, context, traits) { }

        protected internal override object UnpackFromCore( Unpacker unpacker )
        {
            if ( !unpacker.IsArrayHeader )
            {
                throw SerializationExceptions.NewIsNotArrayHeader();
            }

            var instance = Array.CreateInstance( this.Traits.ElementType, UnpackHelpers.GetItemsCount( unpacker ) );
            this.UnpackToCore( unpacker, instance );
            return instance;
        }
    }
}
#endif