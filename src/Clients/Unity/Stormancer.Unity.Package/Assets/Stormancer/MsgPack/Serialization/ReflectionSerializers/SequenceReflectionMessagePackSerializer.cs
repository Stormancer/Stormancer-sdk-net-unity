#if UNITY_IOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Text;
using MsgPack.Serialization.Metadata;

namespace MsgPack.Serialization.ReflectionSerializers
{
    internal abstract class SequenceReflectionMessagePackSerializer : MessagePackSerializer
    {
        private readonly Func<object, int> _getCount;
        private readonly CollectionTraits _traits;

        protected CollectionTraits Traits
        {
            get { return this._traits; }
        }

        private readonly IMessagePackSerializer _elementSerializer;

        private readonly Action<Packer, object, IMessagePackSerializer> _packToCore;
        private readonly Action<Unpacker, object, IMessagePackSerializer> _unpackToCore;

        protected SequenceReflectionMessagePackSerializer(Type type, SerializationContext context, CollectionTraits traits)
            : base(type, (context ?? SerializationContext.Default).CompatibilityOptions.PackerCompatibilityOptions)
        {
            Contract.Assert(type.IsArray || typeof(IEnumerable).IsAssignableFrom(type), type + " is not array nor IEnumerable");
            this._traits = traits;
            this._elementSerializer = context.GetSerializer(traits.ElementType);
            this._getCount = ReflectionSerializerLogics.CreateGetCount(type, traits);

            //var packerParameter = Expression.Parameter(typeof(Packer), "packer");
            //var objectTreeParameter = Expression.Parameter(typeof(T), "objectTree");
            //var elementSerializerParameter = Expression.Parameter(typeof(IMessagePackSerializer), "elementSerializer");

            this._packToCore = (Packer packer, object objectTree, IMessagePackSerializer elementSerializer) =>
                {
                    var length = this._getCount(objectTree);
                    packer.PackArrayHeader(length);
                    foreach (var item in (IEnumerable)objectTree)
                    {
                        elementSerializer.PackTo(packer, item);
                    }
                };


            /*
             *	for ( int i = 0; i < count; i++ )
             *	{
             *		if ( !unpacker.Read() )
             *		{
             *			throw SerializationExceptions.NewMissingItem( i );
             *		}
             *	
             *		T item;
             *		if ( !unpacker.IsArrayHeader && !unpacker.IsMapHeader )
             *		{
             *			item = this.ElementSerializer.UnpackFrom( unpacker );
             *		}
             *		else
             *		{
             *			using ( Unpacker subtreeUnpacker = unpacker.ReadSubtree() )
             *			{
             *				item = this.ElementSerializer.UnpackFrom( subtreeUnpacker );
             *			}
             *		}
             *
             *		instance[ i ] = item; -- OR -- instance.Add( item );
             *	}
             */

            // FIXME: use UnpackHelper

            if (type.IsArray)
            {
                var arrayUnpackerMethod = _UnpackHelpers.UnpackArrayTo_1.MakeGenericMethod(traits.ElementType);
                this._unpackToCore = (Unpacker unpacker, object instance, IMessagePackSerializer elementSerializer) =>
                {
                    arrayUnpackerMethod.Invoke(null, new object[] { unpacker, elementSerializer, instance });
                };
            }
            else
            {
                this._unpackToCore = (Unpacker unpacker, object instance, IMessagePackSerializer elementSerializer) =>
                {
                    var count = UnpackHelpers.GetItemsCount(unpacker);
                    for (int i = 0; i < count; i++)
                    {
                        if (!unpacker.Read())
                        {
                            throw SerializationExceptions.NewMissingItem(i);
                        }
                        object item;
                        if (!unpacker.IsArrayHeader && !unpacker.IsMapHeader)
                        {
                            item = elementSerializer.UnpackFrom(unpacker);
                        }
                        else
                        {
                            using (Unpacker subtreeUnpacker = unpacker.ReadSubtree())
                            {
                                item = elementSerializer.UnpackFrom(subtreeUnpacker);
                            }
                        }
                        traits.AddMethod.Invoke(instance, new object[] { item });
                    }
                };
            }
        }

        protected internal override void PackToCore(Packer packer, object objectTree)
        {
            this._packToCore(packer, objectTree, this._elementSerializer);
        }

        protected internal override void UnpackToCore(Unpacker unpacker, object collection)
        {
            this._unpackToCore(unpacker, collection, this._elementSerializer);
        }
    }
}
#endif