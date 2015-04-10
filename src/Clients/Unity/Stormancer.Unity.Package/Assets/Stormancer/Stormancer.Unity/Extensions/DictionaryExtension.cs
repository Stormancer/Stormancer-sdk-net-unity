using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer
{
    public static class DictionaryExtension
    {
        public static TValue AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            TValue oldValue;
            var keyAlreadyExist = dictionary.TryGetValue(key, out oldValue);
            if (!keyAlreadyExist)
            {
                try
                {
                    dictionary.Add(key, addValue);
                    return addValue;
                }
                catch (ArgumentException)
                {
                }
            }

            var newValue = updateValueFactory(key, oldValue);
            dictionary[key] = newValue;
            return newValue;
        }
    }
}
