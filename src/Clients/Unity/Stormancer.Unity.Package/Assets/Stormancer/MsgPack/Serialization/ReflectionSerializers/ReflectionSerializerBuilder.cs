#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgPack.Serialization.ReflectionSerializers
{
    internal sealed class ReflectionSerializerBuilder<TObject> : SerializerBuilder<TObject>
    {
        public ReflectionSerializerBuilder(SerializationContext context)
            : base(context)
        { }

        protected override MessagePackSerializer<TObject> CreateSerializer(SerializingMember[] entries)
        {

            if (this.Context.SerializationMethod == SerializationMethod.Array)
            {
                return new MessagePackSerializer<TObject>(new ArrayFormatObjectRecflectionMessagePackSerializer(typeof(TObject), this.Context, entries));
            }
            else
            {
                return new MessagePackSerializer<TObject>(new MapFormatObjectReflectionMessagePackSerializer(typeof(TObject), this.Context, entries));
            }
        }

        public override MessagePackSerializer<TObject> CreateArraySerializer()
        {

            var traits = typeof(TObject).GetCollectionTraits();
            if (typeof(TObject).IsArray)
            {
                return new MessagePackSerializer<TObject>(new ArrayRecflectionMessagePackSerializer(typeof(TObject), this.Context, traits));
            }
            else
            {
                return new MessagePackSerializer<TObject>(new ListReflectionMessagePackSerializer(typeof(TObject), this.Context, traits));
            }
        }

        public override MessagePackSerializer<TObject> CreateMapSerializer()
        {
            return new MessagePackSerializer<TObject>(new MapReflectionMessagePackSerializer(typeof(TObject), this.Context, typeof(TObject).GetCollectionTraits()));
        }

        public override MessagePackSerializer<TObject> CreateTupleSerializer()
        {
            throw new PlatformNotSupportedException();
        }
    }
}
#endif