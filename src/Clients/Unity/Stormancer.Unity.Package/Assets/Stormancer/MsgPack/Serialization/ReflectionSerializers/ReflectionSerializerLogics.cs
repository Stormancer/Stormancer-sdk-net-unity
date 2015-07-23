#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MsgPack.Serialization.ReflectionSerializers
{
    internal static class ReflectionSerializerLogics
    {
        public static Func<object, int> CreateGetCount(Type type, CollectionTraits traits)
        {
            if (type.IsArray)
            {
                var lengthAccessor = type.GetProperty("Length").GetGetMethod();
                return t => (int)lengthAccessor.Invoke(t, new object[0]);
            }
            else if (traits.CountProperty != null)
            {
                var countAccessor = traits.CountProperty.GetGetMethod();
                return t => (int)countAccessor.Invoke(t, new object[0]);
            }
            else
            {
                var countMethod = Metadata._Enumerable.Count1Method.MakeGenericMethod(traits.ElementType);
                return t => (int)countMethod.Invoke(null, new object[] {t});
            }
        }

        private static readonly Type[] _containsCapacity = new[] { typeof(int) };

        /// <summary>
        ///		Returns an appropriate <see cref="ConstructorInfo"/> of collection.
        /// </summary>
        /// <param name="context">The serialization context which holds default collection type.</param>
        /// <param name="type">The type of the collection.</param>
        /// <returns>An appropriate <see cref="ConstructorInfo"/> of collection.</returns>
        /// <remarks>
        ///		If the collection has <c>.ctor(int capacity)</c>, then it will be returned.
        ///		Otherwise, default constructor will be returned.
        ///		Note that this method cannot determine whether a single <see cref="Int32"/> parameter truely represents 'capacity' or not.
        /// </remarks>
        public static ConstructorInfo GetCollectionConstructor(SerializationContext context, Type type)
        {
            return type.GetConstructor(_containsCapacity) ?? type.GetConstructor(ReflectionAbstractions.EmptyTypes);
        }
    }
}
#endif