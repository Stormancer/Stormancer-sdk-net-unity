#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2012 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
#if UNITY_IOS
using MsgPack.Serialization.ReflectionSerializers;
using System.IO;
using System.Globalization;
#else
using MsgPack.Serialization.EmittingSerializers;
#endif

namespace MsgPack.Serialization
{
    /// <summary>
    ///		Defines entry points for <see cref="MessagePackSerializer{T}"/> usage.
    /// </summary>
#if UNITY_IOS
    public abstract class MessagePackSerializer : IMessagePackSerializer, IMessagePackSingleObjectSerializer
#else
	public static class MessagePackSerializer
#endif
    {
        #region static
        /// <summary>
        ///		Creates new <see cref="MessagePackSerializer{T}"/> instance with <see cref="SerializationContext.Default"/>.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <returns>
        ///		New <see cref="MessagePackSerializer{T}"/> instance to serialize/deserialize the object tree which the top is <typeparamref name="T"/>.
        /// </returns>
        public static MessagePackSerializer<T> Create<T>()
        {
            Contract.Ensures(Contract.Result<MessagePackSerializer<T>>() != null);

            return MessagePackSerializer.Create<T>(SerializationContext.Default);
        }

        /// <summary>
        ///		Creates new <see cref="MessagePackSerializer{T}"/> instance with specified <see cref="SerializationContext"/>.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="context">
        ///		<see cref="SerializationContext"/> to store known/created serializers.
        /// </param>
        /// <returns>
        ///		New <see cref="MessagePackSerializer{T}"/> instance to serialize/deserialize the object tree which the top is <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="context"/> is <c>null</c>.
        /// </exception>
        public static MessagePackSerializer<T> Create<T>(SerializationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            Contract.Ensures(Contract.Result<MessagePackSerializer<T>>() != null);

#if UNITY_IOS
            Func<SerializationContext, ISerializerBuilder> builderProvider = c => new ReflectionSerializerBuilder<T>(c);

            return new MessagePackSerializer<T>(new AutoMessagePackSerializer(typeof(T), context, builderProvider));
#else
			Func<SerializationContext, SerializerBuilder<T>> builderProvider;
#if NETFX_CORE
			builderProvider = c => new ExpressionSerializerBuilder<T>( c );
#else
				if ( context.SerializationMethod == SerializationMethod.Map )
				{
					builderProvider = c => new MapEmittingSerializerBuilder<T>( c );
				}
				else
				{
					builderProvider = c => new ArrayEmittingSerializerBuilder<T>( c );
				}
#endif // NETFX_CORE else

				return new AutoMessagePackSerializer<T>( context, builderProvider );
#endif // UNITY_IOS else
        }

        private static readonly object _syncRoot = new object();
        private static readonly Dictionary<Type, Func<SerializationContext, IMessagePackSingleObjectSerializer>> _creatorCache = new Dictionary<Type, Func<SerializationContext, IMessagePackSingleObjectSerializer>>();

        /// <summary>
        ///		Creates new <see cref="IMessagePackSerializer"/> instance with <see cref="SerializationContext.Default"/>.
        /// </summary>
        /// <param name="targetType">Target type.</param>
        /// <returns>
        ///		New <see cref="IMessagePackSingleObjectSerializer"/> instance to serialize/deserialize the object tree which the top is <paramref name="targetType"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="targetType"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        ///		To avoid boxing and strongly typed API is prefered, use <see cref="Create{T}()"/> instead when possible.
        /// </remarks>
        public static IMessagePackSingleObjectSerializer Create(Type targetType)
        {
            return MessagePackSerializer.Create(targetType, SerializationContext.Default);
        }

        /// <summary>
        ///		Creates new <see cref="IMessagePackSerializer"/> instance with specified <see cref="SerializationContext"/>.
        /// </summary>
        /// <param name="targetType">Target type.</param>
        /// <param name="context">
        ///		<see cref="SerializationContext"/> to store known/created serializers.
        /// </param>
        /// <returns>
        ///		New <see cref="IMessagePackSingleObjectSerializer"/> instance to serialize/deserialize the object tree which the top is <paramref name="targetType"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="targetType"/> is <c>null</c>.
        ///		Or, <paramref name="context"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        ///		To avoid boxing and strongly typed API is prefered, use <see cref="Create{T}(SerializationContext)"/> instead when possible.
        /// </remarks>
        public static IMessagePackSingleObjectSerializer Create(Type targetType, SerializationContext context)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException("targetType");
            }

            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            Contract.Ensures(Contract.Result<IMessagePackSerializer>() != null);

            Func<SerializationContext, IMessagePackSingleObjectSerializer> factory;

            lock (_syncRoot)
            {
                _creatorCache.TryGetValue(targetType, out factory);
            }

            if (factory == null)
            {
                // Utilize covariance of delegate.
                factory =
                    Delegate.CreateDelegate(
                        typeof(Func<SerializationContext, IMessagePackSingleObjectSerializer>),
                        Metadata._MessagePackSerializer.Create1_Method.MakeGenericMethod(targetType)
                        ) as Func<SerializationContext, IMessagePackSingleObjectSerializer>;

                Contract.Assert(factory != null);

                lock (_syncRoot)
                {
                    _creatorCache[targetType] = factory;
                }
            }
            return factory(context);
        }

        #endregion

#if UNITY_IOS
         private readonly bool _isNullable;
        private readonly Type _type;
        public Type TargetType { get { return _type; } }

        private readonly PackerCompatibilityOptions _packerCompatibilityOptions;

        /// <summary>
        ///		Initializes a new instance of the <see cref="MessagePackSerializer{T}"/> class with <see cref="PackerCompatibilityOptions.Classic"/>.
        /// </summary>
        protected MessagePackSerializer(Type type) : this(type, PackerCompatibilityOptions.Classic) { }

        /// <summary>
        ///		Initializes a new instance of the <see cref="MessagePackSerializer{T}"/> class.
        /// </summary>
        /// <param name="packerCompatibilityOptions">The <see cref="PackerCompatibilityOptions"/> for new packer creation.</param>
        protected MessagePackSerializer(Type type, PackerCompatibilityOptions packerCompatibilityOptions)
        {
            this._type = type;
            this._isNullable = this.JudgeNullable();
            this._packerCompatibilityOptions = packerCompatibilityOptions;
        }

        private bool JudgeNullable()
        {
            if (!_type.GetIsValueType())
            {
                // reference type.
                return true;
            }

            if (_type == typeof(MessagePackObject))
            {
                // can be MPO.Nil.
                return true;
            }

            if (_type.GetIsGenericType() && _type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // Nullable<T>
                return true;
            }

            return false;
        }

        /// <summary>
        ///		Serialize specified object to the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Destination <see cref="Stream"/>.</param>
        /// <param name="objectTree">Object to be serialized.</param>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="stream"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="SerializationException">
        ///		<typeparamref name="T"/> is not serializable etc.
        /// </exception>
        public void Pack(Stream stream, object objectTree)
        {
            this.PackTo(Packer.Create(stream, this._packerCompatibilityOptions), objectTree);
        }

        /// <summary>
        ///		Deserialize object from the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">Source <see cref="Stream"/>.</param>
        /// <returns>Deserialized object.</returns>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="stream"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="SerializationException">
        ///		<typeparamref name="T"/> is not serializable etc.
        /// </exception>
        public object Unpack(Stream stream)
        {
            var unpacker = Unpacker.Create(stream);
            unpacker.Read();
            return this.UnpackFrom(unpacker);
        }

        /// <summary>
        ///		Serialize specified object with specified <see cref="Packer"/>.
        /// </summary>
        /// <param name="packer"><see cref="Packer"/> which packs values in <paramref name="objectTree"/>.</param>
        /// <param name="objectTree">Object to be serialized.</param>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="packer"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="SerializationException">
        ///		<typeparamref name="T"/> is not serializable etc.
        /// </exception>
        public void PackTo(Packer packer, object objectTree)
        {
            // TODO: Hot-Path-Optimization
            if (packer == null)
            {
                throw new ArgumentNullException("packer");
            }

            if (objectTree == null)
            {
                packer.PackNull();
                return;
            }

            this.PackToCore(packer, objectTree);
        }

        /// <summary>
        ///		Serialize specified object with specified <see cref="Packer"/>.
        /// </summary>
        /// <param name="packer"><see cref="Packer"/> which packs values in <paramref name="objectTree"/>. This value will not be <c>null</c>.</param>
        /// <param name="objectTree">Object to be serialized.</param>
        /// <exception cref="SerializationException">
        ///		<typeparamref name="T"/> is not serializable etc.
        /// </exception>
        protected internal abstract void PackToCore(Packer packer, object objectTree);

        /// <summary>
        ///		Deserialize object with specified <see cref="Unpacker"/>.
        /// </summary>
        /// <param name="unpacker"><see cref="Unpacker"/> which unpacks values of resulting object tree.</param>
        /// <returns>Deserialized object.</returns>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="unpacker"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="SerializationException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="MessageTypeException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="InvalidMessagePackStreamException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///		<typeparamref name="T"/> is abstract type.
        /// </exception>
        public object UnpackFrom(Unpacker unpacker)
        {
            // TODO: Hot-Path-Optimization
            if (unpacker == null)
            {
                throw new ArgumentNullException("unpacker");
            }

            if (unpacker.LastReadData.IsNil)
            {
                if (_isNullable)
                {
                    // null
                    if (_type.IsValueType)
                    {
                        return Activator.CreateInstance(_type);
                    }
                    else
                    {
                        return null;
                    };
                }
                else
                {
                    throw SerializationExceptions.NewValueTypeCannotBeNull(_type);
                }
            }

            return this.UnpackFromCore(unpacker);
        }

        /// <summary>
        ///		Deserialize object with specified <see cref="Unpacker"/>.
        /// </summary>
        /// <param name="unpacker"><see cref="Unpacker"/> which unpacks values of resulting object tree. This value will not be <c>null</c>.</param>
        /// <returns>Deserialized object.</returns>
        /// <exception cref="SerializationException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="MessageTypeException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="InvalidMessagePackStreamException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///		<typeparamref name="T"/> is abstract type.
        /// </exception>
        protected internal abstract object UnpackFromCore(Unpacker unpacker);

        /// <summary>
        ///		Deserialize collection items with specified <see cref="Unpacker"/> and stores them to <paramref name="collection"/>.
        /// </summary>
        /// <param name="unpacker"><see cref="Unpacker"/> which unpacks values of resulting object tree.</param>
        /// <param name="collection">Collection that the items to be stored.</param>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="unpacker"/> is <c>null</c>.
        ///		Or <paramref name="collection"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="SerializationException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="MessageTypeException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="InvalidMessagePackStreamException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///		<typeparamref name="T"/> is not collection.
        /// </exception>
        public void UnpackTo(Unpacker unpacker, object collection)
        {
            // TODO: Hot-Path-Optimization
            if (unpacker == null)
            {
                throw new ArgumentNullException("unpacker");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("unpacker");
            }

            if (unpacker.LastReadData.IsNil)
            {
                return;
            }

            this.UnpackToCore(unpacker, collection);
        }

        /// <summary>
        ///		Deserialize collection items with specified <see cref="Unpacker"/> and stores them to <paramref name="collection"/>.
        /// </summary>
        /// <param name="unpacker"><see cref="Unpacker"/> which unpacks values of resulting object tree. This value will not be <c>null</c>.</param>
        /// <param name="collection">Collection that the items to be stored. This value will not be <c>null</c>.</param>
        /// <exception cref="SerializationException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///		<typeparamref name="T"/> is not collection.
        /// </exception>
        protected internal virtual void UnpackToCore(Unpacker unpacker, object collection)
        {
            throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, "This operation is not supported by '{0}'.", this.GetType()));
        }

        /// <summary>
        ///		Serialize specified object to the array of <see cref="Byte"/>.
        /// </summary>
        /// <param name="objectTree">Object to be serialized.</param>
        /// <returns>An array of <see cref="Byte"/> which stores serialized value.</returns>
        /// <exception cref="SerializationException">
        ///		<typeparamref name="T"/> is not serializable etc.
        /// </exception>
        public byte[] PackSingleObject(object objectTree)
        {
            using (var buffer = new MemoryStream())
            {
                this.Pack(buffer, objectTree);
                return buffer.ToArray();
            }
        }

        /// <summary>
        ///		Deserialize a single object from the array of <see cref="Byte"/> which contains a serialized object.
        /// </summary>
        /// <param name="buffer">An array of <see cref="Byte"/> serialized value to be stored.</param>
        /// <returns>A bytes of serialized binary.</returns>
        /// <exception cref="ArgumentNullException">
        ///		<paramref name="buffer"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="SerializationException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="MessageTypeException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <exception cref="InvalidMessagePackStreamException">
        ///		Failed to deserialize object due to invalid unpacker state, stream content, or so.
        /// </exception>
        /// <remarks>
        ///		<para>
        ///			This method assumes that <paramref name="buffer"/> contains single serialized object dedicatedly,
        ///			so this method does not return any information related to actual consumed bytes.
        ///		</para>
        ///		<para>
        ///			This method is a counter part of <see cref="PackSingleObject"/>.
        ///		</para>
        /// </remarks>
        public object UnpackSingleObject(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            using (var stream = new MemoryStream(buffer))
            {
                return this.Unpack(stream);
            }
        }

        void IMessagePackSerializer.PackTo(Packer packer, object objectTree)
        {
            // TODO: Hot-Path-Optimization
            if (packer == null)
            {
                throw new ArgumentNullException("packer");
            }

            if (objectTree == null)
            {
                if (_type.GetIsValueType())
                {
                    if (!(_type.GetIsGenericType() && _type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    {
                        throw SerializationExceptions.NewValueTypeCannotBeNull(_type);
                    }
                }

                packer.PackNull();
                return;
            }
            else
            {
                if (!(objectTree.GetType().Equals(_type)))
                {
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "'{0}' is not compatible for '{1}'.", objectTree.GetType(), _type), "objectTree");
                }
            }

            this.PackToCore(packer, objectTree);
        }

        object IMessagePackSerializer.UnpackFrom(Unpacker unpacker)
        {
            return this.UnpackFrom(unpacker);
        }

        void IMessagePackSerializer.UnpackTo(Unpacker unpacker, object collection)
        {
            // TODO: Hot-Path-Optimization
            if (unpacker == null)
            {
                throw new ArgumentNullException("unpacker");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            if (!(collection.GetType().Equals(_type)))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "'{0}' is not compatible for '{1}'.", collection.GetType(), _type), "collection");
            }

            this.UnpackToCore(unpacker, collection);
        }

        byte[] IMessagePackSingleObjectSerializer.PackSingleObject(object objectTree)
        {
            if (objectTree != null && !objectTree.GetType().Equals(_type))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "'{0}' is not compatible for '{1}'.", objectTree == null ? "(null)" : objectTree.GetType().FullName, _type), "objectTree");
            }

            return this.PackSingleObject(objectTree);
        }

        object IMessagePackSingleObjectSerializer.UnpackSingleObject(byte[] buffer)
        {
            return this.UnpackSingleObject(buffer);
        }
#endif // UNITY_IOS
    }
}
