#if UNITY_IOS

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace MsgPack.Serialization.ReflectionSerializers
{
    /// <summary>
    ///		Implements expression tree based serializer for general object.
    /// </summary>
    /// <typeparam name="T">The type of target object.</typeparam>
    internal abstract class ObjectReflectionMessagePackSerializer : MessagePackSerializer
    {
        private readonly Func<object, object>[] _memberGetters;

        protected Func<object, object>[] MemberGetters
        {
            get { return this._memberGetters; }
        }

        private readonly MemberSetter[] _memberSetters;

        private readonly IMessagePackSerializer[] _memberSerializers;

        protected IMessagePackSerializer[] MemberSerializers
        {
            get { return this._memberSerializers; }
        }

        private readonly NilImplication[] _nilImplications;
        private readonly bool[] _isCollection;
        private readonly string[] _memberNames;

        protected string[] MemberNames
        {
            get { return this._memberNames; }
        }

        private readonly Dictionary<string, int> _indexMap;

        private readonly Func<object> _createInstance;
        private readonly Action<object, Packer, PackingOptions> _packToMessage;
        private readonly UnpackFromMessageInvocation _unpackFromMessage;

        protected ObjectReflectionMessagePackSerializer(Type type, SerializationContext context, SerializingMember[] members)
            : base(type, (context ?? SerializationContext.Default).CompatibilityOptions.PackerCompatibilityOptions)
        {
            if (type.GetIsAbstract() || type.GetIsInterface())
            {
                throw SerializationExceptions.NewNotSupportedBecauseCannotInstanciateAbstractType(type);
            }

                this._createInstance = () => Activator.CreateInstance(type);

            //Expression.Lambda<Func<T>>(
            //    typeof(T).GetIsValueType()
            //        ? Expression.Default(typeof(T)) as Expression
            //        : Expression.New(typeof(T).GetConstructor(ReflectionAbstractions.EmptyTypes))
            //    ).Compile();
            var isPackable = typeof(IPackable).IsAssignableFrom(type);
            var isUnpackable = typeof(IUnpackable).IsAssignableFrom(type);

            if (isPackable && isUnpackable)
            {
                this._memberSerializers = null;
                this._indexMap = null;
                this._isCollection = null;
                this._nilImplications = null;
                this._memberNames = null;
            }
            else
            {
                this._memberSerializers =
                    members.Select(
                        m => m.Member == null ? NullSerializer.Instance : context.GetSerializer(m.Member.GetMemberValueType())).ToArray
                        (

                        );
                this._indexMap =
                    members
                        .Select((m, i) => new KeyValuePair<SerializingMember, int>(m, i))
                        .Where(kv => kv.Key.Member != null)
                        .ToDictionary(kv => kv.Key.Contract.Name, kv => kv.Value);

                this._isCollection =
                    members.Select(
                        m => m.Member == null ? CollectionTraits.NotCollection : m.Member.GetMemberValueType().GetCollectionTraits()).
                        Select(t => t.CollectionType != CollectionKind.NotCollection).ToArray();

                // NilImplication validity check
                foreach (var member in members)
                {
                    switch (member.Contract.NilImplication)
                    {
                        case NilImplication.Null:
                            {
                                if (member.Member.GetMemberValueType().GetIsValueType() &&
                                    Nullable.GetUnderlyingType(member.Member.GetMemberValueType()) == null)
                                {
                                    throw SerializationExceptions.NewValueTypeCannotBeNull(
                                        member.Contract.Name, member.Member.GetMemberValueType(), member.Member.DeclaringType
                                    );
                                }

                                if (!member.Member.CanSetValue())
                                {
                                    throw SerializationExceptions.NewReadOnlyMemberItemsMustNotBeNull(member.Contract.Name);
                                }

                                break;
                            }
                    }
                }

                this._nilImplications = members.Select(m => m.Contract.NilImplication).ToArray();
                this._memberNames = members.Select(m => m.Contract.Name).ToArray();
            }

            if (isPackable)
            {
                this._packToMessage = (target, packer, packingOptions) =>
                {
                    ((IPackable)target).PackToMessage(packer, packingOptions);
                    //typeof(T).GetInterfaceMap(typeof(IPackable)).TargetMethods.Single().Invoke(target, new object[] { packer, packingOptions });
                };
                this._memberGetters = null;
            }
            else
            {
                this._packToMessage = null;
                this._memberGetters =
                    members.Select<SerializingMember,Func<object,object>>(
                    m => m.Member == null ? (target => null)
                        : CreateMemberGetter(m)).ToArray();
            }

            if (isUnpackable)
            {
                this._unpackFromMessage = delegate(ref object target, Unpacker value)
                {
                    ((IUnpackable)target).UnpackFromMessage(value);
                };

                this._memberSetters = null;
            }
            else
            {
                this._unpackFromMessage = null;

                this._memberSetters =
                    members.Select(
                        m =>
                        m.Member == null
                        ? delegate(ref object target, object memberValue) { }
                :
                  m.Member.CanSetValue()
                        ? CreateMemberSetter(m)
                        : UnpackHelpers.IsReadOnlyAppendableCollectionMember(m.Member)
                        ? default(MemberSetter)
                        : ThrowGetOnlyMemberIsInvalid(m.Member)
                ).ToArray();
            }
        }
        private static Func<object, object> CreateMemberGetter(SerializingMember member)
        {
            Func<object, object> coreGetter;

            var fieldInfo = member.Member as FieldInfo; 

            if (fieldInfo != null)
            {
                coreGetter = target => fieldInfo.GetValue(target);
            }
            else
            {
                var propertyInfo = member.Member as PropertyInfo;
                Contract.Assert(propertyInfo != null, member.ToString() + ":" + member.GetType());
                var propertyGetterInfo = propertyInfo.GetGetMethod();
                coreGetter = target => propertyGetterInfo.Invoke(target, new object[0]);
            }

            if (member.Contract.NilImplication == NilImplication.Prohibit)
            {
                var name = member.Contract.Name;
                return target =>
                {
                    var gotten = coreGetter(target);
                    if (gotten == null)
                    {
                        throw SerializationExceptions.NewNullIsProhibited(name);
                    }

                    return gotten;
                };
            }
            else
            {
                return coreGetter;
            }
        }

        private MemberSetter CreateMemberSetter(SerializingMember member)
        {
            var fieldInfo = member.Member as FieldInfo;

            if (fieldInfo != null)
            {
                return delegate(ref object target, object memberValue)
                {
                    fieldInfo.SetValue(target, memberValue);
                };
            }
            else
            {
                var propertyInfo = member.Member as PropertyInfo;
                Contract.Assert(propertyInfo != null, member.ToString() + ":" + member.GetType());
                var propertySetter = propertyInfo.GetSetMethod();

                return delegate(ref object target, object memberValue)
                {
                    propertySetter.Invoke(target, new object[] { memberValue });
                };
            }


            // Expression.Lambda<MemberSetter>(
            //    Expression.Assign(
            //        Expression.PropertyOrField(
            //            refTargetParameter,
            //            m.Member.Name
            //        ),
            //        Expression.Call(
            //            Metadata._UnpackHelpers.ConvertWithEnsuringNotNull_1Method.MakeGenericMethod(m.Member.GetMemberValueType()),
            //            valueParameter,
            //            Expression.Constant(m.Member.Name),
            //            Expression.Call( // Using RuntimeTypeHandle to avoid WinRT expression tree issue.
            //                null,
            //                Metadata._Type.GetTypeFromHandle,
            //                Expression.Constant(m.Member.DeclaringType.TypeHandle)
            //            )
            //        )
            //    ),
            //    refTargetParameter,
            //    valueParameter
            //).Compile()
        }

        private static MemberSetter ThrowGetOnlyMemberIsInvalid(MemberInfo member)
        {
            var asProperty = member as PropertyInfo;
            if (asProperty != null)
            {
                throw new SerializationException(String.Format(CultureInfo.CurrentCulture, "Cannot set value to '{0}.{1}' property.", asProperty.DeclaringType, asProperty.Name));
            }
            else
            {
                Contract.Assert(member is FieldInfo, member.ToString() + ":" + member.GetType());
                throw new SerializationException(
                    String.Format(
                        CultureInfo.CurrentCulture, "Cannot set value to '{0}.{1}' field.", member.DeclaringType, member.Name
                    )
                );
            }
        }

        protected internal sealed override void PackToCore(Packer packer, object objectTree)
        {
            if (this._packToMessage != null)
            {
                this._packToMessage(objectTree, packer, null);
            }
            else
            {
                this.PackToCoreOverride(packer, objectTree);
            }
        }

        protected abstract void PackToCoreOverride(Packer packer, object objectTree);

        protected internal override object UnpackFromCore(Unpacker unpacker)
        {
            // Assume subtree unpacker
            var instance = this._createInstance();

            if (this._unpackFromMessage != null)
            {
                this._unpackFromMessage(ref instance, unpacker);
            }
            else
            {
                if (unpacker.IsArrayHeader)
                {
                    this.UnpackFromArray(unpacker, ref instance);
                }
                else
                {
                    this.UnpackFromMap(unpacker, ref instance);
                }
            }

            return instance;
        }

        private void UnpackFromArray(Unpacker unpacker, ref object instance)
        {
            int unpacked = 0;
            int itemsCount = checked((int)unpacker.ItemsCount);
            for (int i = 0; i < this.MemberSerializers.Length; i++)
            {
                if (unpacked == itemsCount)
                {
                    // It is OK to avoid skip missing member because default NilImplication is MemberDefault so it is harmless.
                    this.HandleNilImplication(ref instance, i);
                }
                else
                {
                    if (!unpacker.Read())
                    {
                        throw SerializationExceptions.NewUnexpectedEndOfStream();
                    }

                    if (unpacker.LastReadData.IsNil)
                    {
                        this.HandleNilImplication(ref instance, i);
                    }
                    else
                    {
                        if (unpacker.IsArrayHeader || unpacker.IsMapHeader)
                        {
                            using (var subtreeUnpacker = unpacker.ReadSubtree())
                            {
                                this.UnpackMemberInArray(subtreeUnpacker, ref instance, i);
                            }
                        }
                        else
                        {
                            this.UnpackMemberInArray(unpacker, ref instance, i);
                        }
                    }

                    unpacked++;
                }
            }
        }

        private void HandleNilImplication(ref object instance, int index)
        {
            switch (this._nilImplications[index])
            {
                case NilImplication.Null:
                    {
                        this._memberSetters[index](ref instance, null);
                        break;
                    }
                case NilImplication.MemberDefault:
                    {
                        break;
                    }
                case NilImplication.Prohibit:
                    {
                        throw SerializationExceptions.NewNullIsProhibited(this._memberNames[index]);
                    }
            }
        }

        private void UnpackMemberInArray(Unpacker unpacker, ref object instance, int i)
        {
            if (this._memberSetters[i] == null)
            {
                // Use null as marker because index mapping cannot be constructed in the constructor.
                this._memberSerializers[i].UnpackTo(unpacker, this._memberGetters[i](instance));
            }
            else
            {
                this._memberSetters[i](ref instance, this._memberSerializers[i].UnpackFrom(unpacker));
            }
        }

        private void UnpackFromMap(Unpacker unpacker, ref object instance)
        {
            while (unpacker.Read())
            {
                var memberName = GetMemberName(unpacker);
                int index;
                if (!this._indexMap.TryGetValue(memberName, out index))
                {
                    // Drains unused value.
                    if (!unpacker.Read())
                    {
                        throw SerializationExceptions.NewUnexpectedEndOfStream();
                    }

                    // TODO: unknown member handling.

                    continue;
                }

                // Fetches value
                if (!unpacker.Read())
                {
                    throw SerializationExceptions.NewUnexpectedEndOfStream();
                }

                if (unpacker.LastReadData.IsNil)
                {
                    switch (this._nilImplications[index])
                    {
                        case NilImplication.Null:
                            {
                                this._memberSetters[index](ref instance, null);
                                continue;
                            }
                        case NilImplication.MemberDefault:
                            {
                                continue;
                            }
                        case NilImplication.Prohibit:
                            {
                                throw SerializationExceptions.NewNullIsProhibited(this._memberNames[index]);
                            }
                    }
                }

                if (unpacker.IsArrayHeader || unpacker.IsMapHeader)
                {
                    using (var subtreeUnpacker = unpacker.ReadSubtree())
                    {
                        this.UnpackMemberInMap(subtreeUnpacker, ref instance, index);
                    }
                }
                else
                {
                    this.UnpackMemberInMap(unpacker, ref instance, index);
                }
            }
        }

        private static string GetMemberName(Unpacker unpacker)
        {
            try
            {
                return unpacker.LastReadData.AsString();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidMessagePackStreamException("Cannot get a member name from stream.", ex);
            }
        }

        private void UnpackMemberInMap(Unpacker unpacker, ref object instance, int index)
        {
            if (this._memberSetters[index] == null)
            {
                // Use null as marker because index mapping cannot be constructed in the constructor.
                this._memberSerializers[index].UnpackTo(unpacker, this._memberGetters[index](instance));
            }
            else
            {
                this._memberSetters[index](ref instance, this._memberSerializers[index].UnpackFrom(unpacker));
            }
        }

        private sealed class NullSerializer : IMessagePackSingleObjectSerializer
        {
            public static readonly NullSerializer Instance = new NullSerializer();

            private NullSerializer()
            {
            }

            public void PackTo(Packer packer, object objectTree)
            {
                if (packer == null)
                {
                    throw new ArgumentNullException("packer");
                }

                Contract.EndContractBlock();

                packer.PackNull();
            }

            public object UnpackFrom(Unpacker unpacker)
            {
                if (unpacker == null)
                {
                    throw new ArgumentNullException("unpacker");
                }

                Contract.Ensures(Contract.Result<object>() == null);

                // Always returns null.
                return null;
            }

            public void UnpackTo(Unpacker unpacker, object collection)
            {
                if (unpacker == null)
                {
                    throw new ArgumentNullException("unpacker");
                }

                if (collection == null)
                {
                    throw new ArgumentNullException("collection");
                }

                throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, "This operation is not supported by '{0}'.", this.GetType()));
            }

            public byte[] PackSingleObject(object objectTree)
            {
                using (var stream = new MemoryStream())
                using (var packer = Packer.Create(stream))
                {
                    this.PackTo(packer, objectTree);
                    return stream.ToArray();
                }
            }

            public object UnpackSingleObject(byte[] buffer)
            {
                using (var stream = new MemoryStream(buffer))
                using (var unpacker = Unpacker.Create(stream))
                {
                    return this.UnpackFrom(unpacker);
                }
            }
        }

        private delegate void UnpackFromMessageInvocation(ref object target, Unpacker value);
        protected delegate void MemberSetter(ref object target, object memberValue);
    }
}
#endif