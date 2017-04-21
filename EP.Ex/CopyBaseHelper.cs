using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace EP.Ex
{
    /// <summary>
    /// Helper of custom object copy
    /// </summary>
    internal static class CopyBaseHelper
    {
        #region Internal Fields

        internal const BindingFlags FInternalStatic = Obj.FInternalStatic;
        internal const BindingFlags FPublicStatic = Obj.FPublicStatic;

        #endregion Internal Fields

        #region Public Methods

        /// <summary>
        /// Get delegate func that create deep copy of object
        /// </summary>
        /// <typeparam name="T">Source object type</typeparam>
        /// <returns>delegate if present, or null</returns>
        public static Func<T, Dictionary<object, object>, T> DeepCopyFunc<T>()
        {
            var mi = m_get_info(typeof(T));
            return mi != null ? (Func<T, Dictionary<object, object>, T>)Delegate.CreateDelegate(typeof(Func<T, Dictionary<object, object>, T>), mi) : null;
        }

        /// <summary>
        /// Helper that make deep copy of Dictionary
        /// </summary>
        /// <typeparam name="K">Type of Key property</typeparam>
        /// <typeparam name="V">Type of Value property</typeparam>
        /// <param name="src">Source dictionary</param>
        /// <param name="dict">
        /// Dictionary, that accumulate copies of objects, to have one object - one copy
        /// </param>
        /// <returns>Deep copy of dictionary</returns>
        public static Dictionary<K, V> m_deep_copy_dict<K, V>(Dictionary<K, V> src, Dictionary<object, object> dict)
        {
            var dst = new Dictionary<K, V>(src.Count, src.Comparer);
            object key;
            object value;
            foreach (var p in src)
            {
                if (!dict.TryGetValue(p.Key, out key))
                {
                    key = Obj.DeepCopy(p.Key, dict);
                }
                if (p.Value != null)
                {
                    if (!dict.TryGetValue(p.Value, out value))
                    {
                        value = Obj.DeepCopy(p.Value, dict);
                    }
                }
                else
                {
                    value = p.Value;
                }
                dst[(K)key] = (V)value;
            }
            return dst;
        }

        /// <summary>
        /// Helper that make deep copy of hashset
        /// </summary>
        /// <typeparam name="T">Type of hashset items</typeparam>
        /// <param name="src">Source hashset</param>
        /// <param name="dic">
        /// Dictionary, that accumulate copies of objects, to have one object - one copy
        /// </param>
        /// <returns>Deep copy of hashset</returns>
        public static HashSet<T> m_deep_copy_hashset<T>(HashSet<T> src, Dictionary<object, object> dict)
        {
            var dst = new HashSet<T>(src.Comparer);
            object value;
            foreach (var p in src)
            {
                if (!dict.TryGetValue(p, out value))
                {
                    value = Obj.DeepCopy((object)p, dict);
                }
                dst.Add((T)value);
            }
            return dst;
        }

        /// <summary>
        /// Helper that make deep copy of Hashtable
        /// </summary>
        /// <param name="src">Source Hashtable</param>
        /// <param name="dict">
        /// Dictionary, that accumulate copies of objects, to have one object - one copy
        /// </param>
        /// <returns>Deep copy of Hashtable</returns>
        public static Hashtable m_deep_copy_hashtable(Hashtable src, Dictionary<object, object> dict)
        {
            var dst = ((Hashtable)src.Clone());//TODO: rewrite
            dst.Clear();
            object key;
            object value;
            foreach (DictionaryEntry p in src)
            {
                if (!dict.TryGetValue(p.Key, out key))
                {
                    key = Obj.DeepCopy(p.Key, dict);
                }
                if (p.Value != null)
                {
                    if (!dict.TryGetValue(p.Value, out value))
                    {
                        value = Obj.DeepCopy(p.Value, dict);
                    }
                }
                else
                {
                    value = p.Value;
                }
                dst[key] = value;
            }
            return dst;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Helper that make deep copy of array
        /// </summary>
        /// <typeparam name="T">Type of Array items</typeparam>
        /// <param name="src">Source array</param>
        /// <param name="dic">
        /// Dictionary, that accumulate copies of objects, to have one object - one copy
        /// </param>
        /// <returns>Deep copy of array</returns>
        private static T[] m_deepcopy_array<T>(T[] src, Dictionary<object, object> dic)
        {
            var t = typeof(T);
            object o;
            T[] dst;
            if (dic.TryGetValue(src, out o))
            {
                return (T[])o;
            }
            dst = new T[src.Length];
            if (t.IsSimple())
            {
                Array.Copy(src, dst, src.Length);
            }
            else
            {
                dic[src] = dst;
                for (int i = 0; i < src.Length; ++i)
                {
                    dst[i] = (T)Obj.m_deepcopy(src[i], dic);
                }
            }
            return dst;
        }

        /// <summary>
        /// Get method of deep object copy
        /// </summary>
        /// <param name="t">Source object type</param>
        /// <returns>Method info if present, else null</returns>
        private static MethodInfo m_get_info(Type t)
        {
            if (t.IsArray)
            {
                var arrt = t.GetElementType();
                return typeof(CopyBaseHelper).GetMethod(nameof(CopyBaseHelper.m_deepcopy_array), FInternalStatic).MakeGenericMethod(arrt);
            }
            if (t == typeof(Hashtable))
            {
                return typeof(CopyBaseHelper).GetMethod(nameof(CopyBaseHelper.m_deep_copy_hashtable), FPublicStatic);
            }
            if (t.IsGenericType)
            {
                var generic = t.GetGenericTypeDefinition();
                if (generic == typeof(Dictionary<,>))
                {
                    Type keyType = t.GetGenericArguments()[0];
                    Type valueType = t.GetGenericArguments()[1];
                    return typeof(CopyBaseHelper).GetMethod(nameof(CopyBaseHelper.m_deep_copy_dict), FPublicStatic).MakeGenericMethod(keyType, valueType);
                }
                else if (generic == typeof(HashSet<>))
                {
                    Type valueType = t.GetGenericArguments()[0];
                    return typeof(CopyBaseHelper).GetMethod(nameof(CopyBaseHelper.m_deep_copy_hashset), FPublicStatic).MakeGenericMethod(valueType);
                }
            }
            return null;
        }

        #endregion Private Methods
    }
}