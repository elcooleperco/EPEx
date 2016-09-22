using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EP.Ex
{
    internal static class CopyBaseHelper
    {
        internal const BindingFlags FInternalStatic = Obj.FInternalStatic;
        internal const BindingFlags FPublicStatic = Obj.FPublicStatic;
        private static MethodInfo m_get_info(Type t)
        {
            if (t.IsArray)
            {
                var arrt = t.GetElementType();
                return typeof(CopyBaseHelper).GetMethod("m_deepcopy_array", FInternalStatic).MakeGenericMethod(arrt);
            }
            if (t == typeof(Hashtable))
            {
                return typeof(CopyBaseHelper).GetMethod("m_deep_copy_hashtable", FPublicStatic);

            }
            if (t.IsGenericType)
            {
                var generic = t.GetGenericTypeDefinition();
                if (generic == typeof(Dictionary<,>))
                {
                    Type keyType = t.GetGenericArguments()[0];
                    Type valueType = t.GetGenericArguments()[1];
                    return typeof(CopyBaseHelper).GetMethod("m_deep_copy_dict", FPublicStatic).MakeGenericMethod(keyType, valueType);
                }
                else if (generic == typeof(HashSet<>))
                {
                    Type valueType = t.GetGenericArguments()[0];
                    return typeof(CopyBaseHelper).GetMethod("m_deep_copy_hashset", FPublicStatic).MakeGenericMethod(valueType);
                }
            }
            return null;
        }
        public static Func<T, Dictionary<object, object>, T> DeepCopyFunc<T>()
        {

            var mi = m_get_info(typeof(T));
            return mi != null ? (Func<T, Dictionary<object, object>, T>)Delegate.CreateDelegate(typeof(Func<T, Dictionary<object, object>, T>), mi) : null;
        }
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
    }
}
