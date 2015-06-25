#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2013 FUJIWARA, Yusuke
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
#if !UNITY_IOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using MsgPack.Serialization.Reflection;

namespace MsgPack.Serialization.EmittingSerializers
{
	// Reduce JIT
	/// <summary>
	///		Defines non-generic functions of <see cref="EmittingSerializerBuilder{T}"/>.
	/// </summary>
	internal static class EmittingSerializerBuilderLogics
	{
		private static readonly Type[] _delegateConstructorParameterTypes = new Type[] { typeof( object ), typeof( IntPtr ) };

		#region -- Arrays --

		public static SerializerEmitter CreateArraySerializerCore( SerializationContext context, Type targetType, EmitterFlavor emitterFlavor )
		{
			Contract.Requires( targetType != null );
			Contract.Ensures( Contract.Result<SerializerEmitter>() != null );

			var emitter = SerializationMethodGeneratorManager.Get().CreateEmitter( targetType, emitterFlavor );
			var traits = targetType.GetCollectionTraits();
			CreatePackArrayProceduresCore( targetType, emitter, traits );
			CreateUnpackArrayProceduresCore( context, targetType, emitter, traits );
			return emitter;
		}

		private static void CreatePackArrayProceduresCore( Type targetType, SerializerEmitter emitter, CollectionTraits traits )
		{
			var il = emitter.GetPackToMethodILGenerator();
			var localHolder = new LocalVariableHolder( il );
			try
			{
				// Array
				if ( targetType.IsArray )
				{
					/*
					 * // array
					 *  packer.PackArrayHeader( length );
					 * for( int i = 0; i < length; i++ )
					 * {
					 * 		this._serializer.PackTo( packer, collection[ i ] );
					 * }
					 */
					var length = localHolder.PackingCollectionCount;
					il.EmitAnyLdarg( 2 );
					il.EmitLdlen();
					il.EmitAnyStloc( length );
					il.EmitAnyLdarg( 1 );
					il.EmitAnyLdloc( length );
					il.EmitAnyCall( Metadata._Packer.PackArrayHeader );
					il.EmitPop();
					Emittion.EmitFor(
						il,
						length,
						( il0, i ) =>
							Emittion.EmitSerializeValue(
								emitter,
								il0,
								1,
								traits.ElementType,
								null,
								NilImplication.MemberDefault,
								il1 =>
								{
									il1.EmitAnyLdarg( 2 );
									il1.EmitAnyLdloc( i );
									il1.EmitLdelem( traits.ElementType );
								},
								localHolder
							)
					);
				}
				else if ( traits.CountProperty == null )
				{
					/*
					 *  array = collection.ToArray();
					 *  packer.PackArrayHeader( length );
					 * for( int i = 0; i < length; i++ )
					 * {
					 * 		this._serializer.PackTo( packer, array[ i ] );
					 * }
					 */
					var array = localHolder.GetSerializingCollection( traits.ElementType.MakeArrayType() );
					EmitLoadTarget( targetType, il, 2 );
					il.EmitAnyCall( Metadata._Enumerable.ToArray1Method.MakeGenericMethod( traits.ElementType ) );
					il.EmitAnyStloc( array );
					var length = localHolder.PackingCollectionCount;
					il.EmitAnyLdloc( array );
					il.EmitLdlen();
					il.EmitAnyStloc( length );
					il.EmitAnyLdarg( 1 );
					il.EmitAnyLdloc( length );
					il.EmitAnyCall( Metadata._Packer.PackArrayHeader );
					il.EmitPop();
					Emittion.EmitFor(
						il,
						length,
						( il0, i ) =>
							Emittion.EmitSerializeValue(
								emitter,
								il0,
								1,
								traits.ElementType,
								null,
								NilImplication.MemberDefault,
								il1 =>
								{
									il1.EmitAnyLdloc( array );
									il1.EmitAnyLdloc( i );
									il1.EmitLdelem( traits.ElementType );
								},
								localHolder
							)
					);
				}
				else
				{
					/*
					 * // Enumerable
					 *  packer.PackArrayHeader( collection.Count );
					 * foreach( var item in list )
					 * {
					 * 		this._serializer.PackTo( packer, array[ i ] );
					 * }
					 */
					var collection = localHolder.GetSerializingCollection( targetType );
					// This instruction always ldarg, not to be ldarga
					il.EmitAnyLdarg( 2 );
					il.EmitAnyStloc( collection );
					var count = localHolder.PackingCollectionCount;
					EmitLoadTarget( targetType, il, 2 );
					il.EmitGetProperty( traits.CountProperty );
					il.EmitAnyStloc( count );
					il.EmitAnyLdarg( 1 );
					il.EmitAnyLdloc( count );
					il.EmitAnyCall( Metadata._Packer.PackArrayHeader );
					il.EmitPop();
					Emittion.EmitForEach(
						il,
						traits,
						collection,
						( il0, getCurrentEmitter ) =>
							Emittion.EmitSerializeValue(
								emitter,
								il0,
								1,
								traits.ElementType,
								null,
								NilImplication.MemberDefault,
								_ => getCurrentEmitter(),
								localHolder
							)
					);
				}
				il.EmitRet();
			}
			finally
			{
				il.FlushTrace();
			}
		}

		private static void CreateUnpackArrayProceduresCore( SerializationContext context, Type targetType, SerializerEmitter emitter, CollectionTraits traits )
		{
			CreateArrayUnpackFrom( context, targetType, emitter, traits );
			CreateArrayUnpackTo( targetType, emitter, traits );
		}

		private static void CreateArrayUnpackFrom( SerializationContext context, Type targetType, SerializerEmitter emitter, CollectionTraits traits )
		{
			var il = emitter.GetUnpackFromMethodILGenerator();
			var localHolder = new LocalVariableHolder( il );
			var instanceType = targetType;

			try
			{
				if ( targetType.IsInterface || targetType.IsAbstract )
				{
					instanceType = context.DefaultCollectionTypes.GetConcreteType( targetType );
					if ( instanceType == null )
					{
						il.EmitTypeOf( targetType );
						il.EmitAnyCall( SerializationExceptions.NewNotSupportedBecauseCannotInstanciateAbstractTypeMethod );
						il.EmitThrow();
						return;
					}
				}

				/*
				 *	if (!unpacker.IsArrayHeader)
				 *	{
				 *		throw SerializationExceptions.NewIsNotArrayHeader();
				 *	}
				 *	
				 *	TCollection collection = new ...;
				 *	this.UnpackToCore(unpacker, array);
				 *	return collection;
				 */

				il.EmitAnyLdarg( 1 );
				il.EmitGetProperty( Metadata._Unpacker.IsArrayHeader );
				var endIf = il.DefineLabel( "END_IF" );
				il.EmitBrtrue_S( endIf );
				il.EmitAnyCall( SerializationExceptions.NewIsNotArrayHeaderMethod );
				il.EmitThrow();
				il.MarkLabel( endIf );
				var collection = localHolder.GetDeserializingCollection( instanceType );
				// Emit newobj, newarr, or call ValueType..ctor()
				Emittion.EmitConstruction(
					il,
					collection,
					il0 => Emittion.EmitGetUnpackerItemsCountAsInt32( il0, 1, localHolder )
				);

				EmitInvokeArrayUnpackToHelper( targetType, emitter, traits, il, 1, il0 => il0.EmitAnyLdloc( collection ) );

				il.EmitAnyLdloc( collection );
				il.EmitRet();
			}
			finally
			{
				il.FlushTrace();
			}
		}

		private static void CreateArrayUnpackTo( Type targetType, SerializerEmitter emitter, CollectionTraits traits )
		{
			var il = emitter.GetUnpackToMethodILGenerator();
			try
			{
				EmitInvokeArrayUnpackToHelper( targetType, emitter, traits, il, 1, il0 => il0.EmitAnyLdarg( 2 ) );
				il.EmitRet();
			}
			finally
			{
				il.FlushTrace();
			}
		}

		private static void EmitInvokeArrayUnpackToHelper( Type targetType, SerializerEmitter emitter, CollectionTraits traits, TracingILGenerator il, int unpackerArgumentIndex, Action<TracingILGenerator> loadCollectionEmitting )
		{
			il.EmitAnyLdarg( unpackerArgumentIndex );
			var serializerGetting = emitter.RegisterSerializer( traits.ElementType );

			if ( targetType.IsArray )
			{
				// Array
				/*
				 * UnpackHelpers.UnpackArrayTo( unpacker, GET_SERIALIZER, collection );
				 */
				serializerGetting( il, 0 );
				loadCollectionEmitting( il );
				il.EmitAnyCall( Metadata._UnpackHelpers.UnpackArrayTo_1.MakeGenericMethod( traits.ElementType ) );
			}
			else if ( targetType.IsGenericType )
			{
				serializerGetting( il, 0 );
				loadCollectionEmitting( il );
				if ( targetType.IsValueType )
				{
					il.EmitBox( targetType );
				}

				if ( traits.AddMethod.ReturnType == null || traits.AddMethod.ReturnType == typeof( void ) )
				{
					// with void Add( T item )
					/*
					 * Action<T> addition = TCollection.Add
					 * UnpackHelpers.UnpackCollectionTo( unpacker, GET_SERIALIZER, collection, addition );
					 */
					var itemType = traits.AddMethod.GetParameters()[ 0 ].ParameterType;
					EmitNewDelegate( il, targetType, traits.AddMethod, loadCollectionEmitting, typeof( Action<> ).MakeGenericType( itemType ) );
					il.EmitAnyCall( Metadata._UnpackHelpers.UnpackCollectionTo_1.MakeGenericMethod( itemType ) );
				}
				else
				{
					// with TDiscarded Add( T item )
					/*
					 * Func<T, TDiscarded> addition = TCollection.Add
					 * UnpackHelpers.UnpackCollectionTo( unpacker, GET_SERIALIZER, collection, addition );
					 */
					var itemType = traits.AddMethod.GetParameters()[ 0 ].ParameterType;
					var discardingType = traits.AddMethod.ReturnType;
					EmitNewDelegate( il, targetType, traits.AddMethod, loadCollectionEmitting, typeof( Func<,> ).MakeGenericType( itemType, discardingType ) );
					il.EmitAnyCall( Metadata._UnpackHelpers.UnpackCollectionTo_2.MakeGenericMethod( itemType, discardingType ) );
				}
			}
			else
			{
				loadCollectionEmitting( il );
				if ( targetType.IsValueType )
				{
					il.EmitBox( targetType );
				}

				if ( traits.AddMethod.ReturnType == null || traits.AddMethod.ReturnType == typeof( void ) )
				{
					// with void Add( object item )
					/*
					 * Action<object> addition = TCollection.Add
					 * UnpackHelpers.UnpackCollectionTo( unpacker, collection, addition );
					 */
					EmitNewDelegate( il, targetType, traits.AddMethod, loadCollectionEmitting, typeof( Action<object> ) );
					il.EmitAnyCall( Metadata._UnpackHelpers.UnpackNonGenericCollectionTo );
				}
				else
				{
					// with TDiscarded Add( object item )
					/*
					 * Func<TDiscarded> addition = TCollection.Add
					 * UnpackHelpers.UnpackCollectionTo( unpacker, collection, addition );
					 */
					var discardingType = traits.AddMethod.ReturnType;
					EmitNewDelegate( il, targetType, traits.AddMethod, loadCollectionEmitting, typeof( Func<,> ).MakeGenericType( typeof( object ), discardingType ) );
					il.EmitAnyCall( Metadata._UnpackHelpers.UnpackNonGenericCollectionTo_1.MakeGenericMethod( discardingType ) );
				}
			}
		}

		private static void EmitNewDelegate( TracingILGenerator il, Type targetType, MethodInfo method, Action<TracingILGenerator> loadTargetEmitting, Type delegateType )
		{
			loadTargetEmitting( il );
			if ( targetType.IsValueType )
			{
				il.EmitBox( targetType );
			}

			if ( method.IsStatic || method.IsFinal || !method.IsVirtual )
			{
				il.EmitLdftn( method );
			}
			else
			{
				il.EmitDup();
				il.EmitLdvirtftn( method );
			}

			il.EmitNewobj( delegateType.GetConstructor( _delegateConstructorParameterTypes ) );
		}

		#endregion -- Arrays --

		#region -- Maps --

		public static SerializerEmitter CreateMapSerializerCore( SerializationContext context, Type targetType, EmitterFlavor emitterFlavor )
		{
			Contract.Requires( targetType != null );
			Contract.Ensures( Contract.Result<SerializerEmitter>() != null );


			var emitter = SerializationMethodGeneratorManager.Get().CreateEmitter( targetType, emitterFlavor );
			var traits = targetType.GetCollectionTraits();
			CreateMapPack(
				targetType,
				emitter,
				traits
			);
			CreateMapUnpack(
				context,
				targetType,
				emitter,
				traits
			);

			return emitter;
		}

		private static void CreateMapPack( Type targetType, SerializerEmitter emiter, CollectionTraits traits )
		{
			var il = emiter.GetPackToMethodILGenerator();
			var localHolder = new LocalVariableHolder( il );
			try
			{

				/*
				 * 	int count = ((ICollection<KeyValuePair<string, DateTime>>)dictionary).Count;
				 * 	packer.PackMapHeader(count);
				 * 	foreach (KeyValuePair<string, DateTime> current in dictionary)
				 * 	{
				 * 		this._serializer0.PackTo(packer, current.Key);
				 * 		this._serializer1.PackTo(packer, current.Value);
				 * 	}
				 */

				var collection = localHolder.GetSerializingCollection( targetType );
				var item = localHolder.GetSerializingCollectionItem( traits.ElementType );
				var keyProperty = traits.ElementType.GetProperty( "Key" );
				var valueProperty = traits.ElementType.GetProperty( "Value" );
				// This instruction is always ldarg, not to be ldarga.
				il.EmitAnyLdarg( 2 );
				il.EmitAnyStloc( collection );
				var count = localHolder.PackingCollectionCount;
				EmitLoadTarget( targetType, il, collection );
				il.EmitGetProperty( traits.CountProperty );
				il.EmitAnyStloc( count );
				il.EmitAnyLdarg( 1 );
				il.EmitAnyLdloc( count );
				il.EmitAnyCall( Metadata._Packer.PackMapHeader );
				il.EmitPop();

				Emittion.EmitForEach(
					il,
					traits,
					collection,
					( il0, getCurrentEmitter ) =>
					{
						if ( traits.ElementType.IsGenericType )
						{
							Contract.Assert( traits.ElementType.GetGenericTypeDefinition() == typeof( KeyValuePair<,> ) );
							getCurrentEmitter();
							il0.EmitAnyStloc( item );
							Emittion.EmitSerializeValue(
								emiter,
								il0,
								1,
								traits.ElementType.GetGenericArguments()[ 0 ],
								null,
								NilImplication.MemberDefault,
								il1 =>
								{
									il1.EmitAnyLdloca( item );
									il1.EmitGetProperty( keyProperty );
								},
								localHolder
							);

							Emittion.EmitSerializeValue(
								emiter,
								il0,
								1,
								traits.ElementType.GetGenericArguments()[ 1 ],
								null,
								NilImplication.MemberDefault,
								il1 =>
								{
									il1.EmitAnyLdloca( item );
									il1.EmitGetProperty( valueProperty );
								},
								localHolder
							);
						}
						else
						{
							Contract.Assert( traits.ElementType == typeof( DictionaryEntry ) );
							getCurrentEmitter();
							il0.EmitAnyStloc( item );
							Emittion.EmitSerializeValue(
								emiter,
								il0,
								1,
								typeof( MessagePackObject ),
								null,
								NilImplication.MemberDefault,
								il1 =>
								{
									il0.EmitAnyLdloca( item );
									il0.EmitGetProperty( Metadata._DictionaryEntry.Key );
									il0.EmitUnbox_Any( typeof( MessagePackObject ) );
								},
								localHolder
							);

							Emittion.EmitSerializeValue(
								emiter,
								il0,
								1,
								typeof( MessagePackObject ),
								null,
								NilImplication.MemberDefault,
								il1 =>
								{
									il0.EmitAnyLdloca( item );
									il0.EmitGetProperty( Metadata._DictionaryEntry.Value );
									il0.EmitUnbox_Any( typeof( MessagePackObject ) );
								},
								localHolder
							);
						}
					}
				);
				il.EmitRet();
			}
			finally
			{
				il.FlushTrace();
			}
		}

		private static void CreateMapUnpack( SerializationContext context, Type targetType, SerializerEmitter emitter, CollectionTraits traits )
		{
			CreateMapUnpackFrom( context, targetType, emitter, traits );
			CreateMapUnpackTo( targetType, emitter, traits );
		}

		private static void CreateMapUnpackFrom( SerializationContext context, Type targetType, SerializerEmitter emitter, CollectionTraits traits )
		{
			var il = emitter.GetUnpackFromMethodILGenerator();
			var localHolder = new LocalVariableHolder( il );
			var instanceType = targetType;

			try
			{
				/*
				 *	if (!unpacker.IsMapHeader)
				 *	{
				 *		throw SerializationExceptions.NewIsNotMapHeader();
				 *	}
				 *	
				 *	TDictionary<TKey, TValue> dictionary = new ...;
				 *	this.UnpackToCore(unpacker, dictionary);
				 *	return dictionary;
				 */

				if ( targetType.IsInterface || targetType.IsAbstract )
				{
					instanceType = context.DefaultCollectionTypes.GetConcreteType( targetType );
					if( instanceType == null )
					{
						il.EmitTypeOf( targetType );
						il.EmitAnyCall( SerializationExceptions.NewNotSupportedBecauseCannotInstanciateAbstractTypeMethod );
						il.EmitThrow();
						return;
					}
				}

				il.EmitAnyLdarg( 1 );
				il.EmitGetProperty( Metadata._Unpacker.IsMapHeader );
				var endIf = il.DefineLabel( "END_IF" );
				il.EmitBrtrue_S( endIf );
				il.EmitAnyCall( SerializationExceptions.NewIsNotMapHeaderMethod );
				il.EmitThrow();
				il.MarkLabel( endIf );

				var collection = localHolder.GetDeserializingCollection( instanceType );
				Emittion.EmitConstruction(
					il,
					collection,
					il0 => Emittion.EmitGetUnpackerItemsCountAsInt32( il0, 1, localHolder )
				);

				EmitInvokeMapUnpackToHelper( targetType, emitter, traits, il, 1, il0 => il0.EmitAnyLdloc( collection ) );

				il.EmitAnyLdloc( collection );
				il.EmitRet();
			}
			finally
			{
				il.FlushTrace();
			}
		}

		private static void CreateMapUnpackTo( Type targetType, SerializerEmitter emitter, CollectionTraits traits )
		{
			var il = emitter.GetUnpackToMethodILGenerator();
			try
			{
				EmitInvokeMapUnpackToHelper( targetType, emitter, traits, il, 1, il0 => il0.EmitAnyLdarg( 2 ) );

				il.EmitRet();
			}
			finally
			{
				il.FlushTrace();
			}
		}


		private static void EmitInvokeMapUnpackToHelper( Type targetType, SerializerEmitter emitter, CollectionTraits traits, TracingILGenerator il, int unpackerArgumentIndex, Action<TracingILGenerator> loadCollectionEmitting )
		{
			il.EmitAnyLdarg( unpackerArgumentIndex );
			if ( traits.ElementType.IsGenericType )
			{
				var keyType = traits.ElementType.GetGenericArguments()[ 0 ];
				var valueType = traits.ElementType.GetGenericArguments()[ 1 ];
				var keySerializerGetting = emitter.RegisterSerializer( keyType );
				var valueSerializerGetting = emitter.RegisterSerializer( valueType );
				keySerializerGetting( il, 0 );
				valueSerializerGetting( il, 0 );
				loadCollectionEmitting( il );

				if ( targetType.IsValueType )
				{
					il.EmitBox( targetType );
				}

				il.EmitAnyCall( Metadata._UnpackHelpers.UnpackMapTo_2.MakeGenericMethod( keyType, valueType ) );
			}
			else
			{
				loadCollectionEmitting( il );

				if ( targetType.IsValueType )
				{
					il.EmitBox( targetType );
				}

				il.EmitAnyCall( Metadata._UnpackHelpers.UnpackNonGenericMapTo );
			}
		}

		#endregion -- Maps --

		#region -- Miscs --

		private static void EmitLoadTarget( Type targetType, TracingILGenerator il, int parameterIndex )
		{
			if ( targetType.IsValueType )
			{
				il.EmitAnyLdarga( parameterIndex );
			}
			else
			{
				il.EmitAnyLdarg( parameterIndex );
			}
		}

		private static void EmitLoadTarget( Type targetType, TracingILGenerator il, LocalBuilder local )
		{
			if ( targetType.IsValueType )
			{
				il.EmitAnyLdloca( local );
			}
			else
			{
				il.EmitAnyLdloc( local );
			}
		}

		#endregion -- Miscs --
	}
}
#endif