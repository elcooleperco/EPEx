using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace EP.Ex
{
    /// <summary>
    /// Class implement object extension
    /// </summary>
    /// <typeparam name="T">Object</typeparam>
    public class Obj<T>// where T : class
    {
        #region Internal Fields

        internal static Func<T> c_tor;
        internal static Func<T, T> m_shallowcopy;

        internal static Func<T, Dictionary<object, object>, T> m_deepcopy;
        internal const BindingFlags FInternalStatic = Obj.FInternalStatic;

        #endregion Internal Fields

        #region Public Constructors

        /// <summary>
        /// Constructor initialise object create func, use constructor without params
        /// </summary>
        static Obj()
        {
            c_tor = m_c_tor_func();
            m_shallowcopy = m_shallowcopy_func();

            //init lazy initialization deep cloning
            m_deepcopy = (x, y) =>
            {
                m_deepcopy = m_deepcopy_func();
                return m_deepcopy(x, y);
            };
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Create new object
        /// </summary>
        /// <returns>new object type T</returns>
        public static T New()
        {
            return c_tor();
        }

        public static T ShallowCopy(T obj)
        {
            return m_shallowcopy(obj);
        }

        #endregion Public Methods

        #region Private Methods

        private static Func<T> m_c_tor_func()
        {
            var t = typeof(T);
            if (t == typeof(object) || t.IsArray)
            {
                return null;
            }
            DynamicMethod creator = new DynamicMethod(string.Empty,
                        t,
                        new Type[] { },
                        t, true);
            ILGenerator il = creator.GetILGenerator();
            Obj.m_create_new_generate(il, t);
            il.Emit(OpCodes.Ret);
            return (Func<T>)creator.CreateDelegate(typeof(Func<T>));
        }

        private static Func<T, T> m_shallowcopy_func()
        {
            var t = typeof(T);
            if (t.IsArray)
            {
                var method = m_arr_copy_mi(t);
                return (Func<T, T>)Delegate.CreateDelegate(typeof(Func<T, T>), method);
            }
            DynamicMethod creator = new DynamicMethod(string.Empty, t, new Type[] { t }, t, true);
            ILGenerator il = creator.GetILGenerator();

            //value type simple return from arg
            if (t.IsValueType)
            {
                il.Emit(OpCodes.Ldarg_S, 0);
            }
            else
            {
                Obj.m_create_uninit_generate(il, t);
                LocalBuilder va = il.DeclareLocal(t);
                il.Emit(OpCodes.Stloc_S, va);
                var flds = Obj.m_get_flds(t);
                if (flds != null && flds.Length > 0)
                {
                    for (int i = 0; i < flds.Length; ++i)
                    {
                        il.Emit(OpCodes.Ldloc_S, va);
                        il.Emit(OpCodes.Ldarg_S, 0);
                        il.Emit(OpCodes.Ldfld, flds[i]);
                        il.Emit(OpCodes.Stfld, flds[i]);
                    }
                }
                il.Emit(OpCodes.Ldloc_S, va);
            }
            il.Emit(OpCodes.Ret);
            return (Func<T, T>)creator.CreateDelegate(typeof(Func<T, T>));
        }

        private static T[] m_deepcopy_array(T[] src, Dictionary<object, object> dic)
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

        private static T[] m_copy_array(T[] src)
        {
            if (src == null)
            {
                return null;
            }
            T[] dst;
            dst = new T[src.Length];
            if (src.Length != 0)
            {
                Array.Copy(src, dst, src.Length);
            }
            return dst;
        }

        private static void m_addtodict(T oldobj, T newobj, Dictionary<object, object> dic)
        {
            dic[oldobj] = newobj;

            //return newobj;
        }

        private static MethodInfo m_arr_deepcopy_mi(Type t)
        {
            var arrt = t.GetElementType();
            return typeof(Obj<>).MakeGenericType(arrt).GetMethod("m_deepcopy_array", FInternalStatic);
        }

        private static MethodInfo m_arr_copy_mi(Type t)
        {
            var arrt = t.GetElementType();
            return typeof(Obj<>).MakeGenericType(arrt).GetMethod("m_copy_array", FInternalStatic);
        }

        public static void SetDeepCopyFn(Func<T, Dictionary<object, object>, T> fn)
        {
            m_deepcopy = fn;
        }

        private static Dictionary<K, V> m_deep_copy_dict<K, V>(Dictionary<K, V> src, Dictionary<object, object> dict)
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

        private static Func<T, Dictionary<object, object>, T> m_deepcopy_func()
        {
            var t = typeof(T);
            if (t.IsArray)
            {
                var method = m_arr_deepcopy_mi(t);
                return (Func<T, Dictionary<object, object>, T>)Delegate.CreateDelegate(typeof(Func<T, Dictionary<object, object>, T>), method);

                //il.Emit(OpCodes.Ldarg_0);//[stack:obj]
                //il.Emit(OpCodes.Ldarg_1);//[stack:obj,dict]
                //il.Emit(OpCodes.Call, method);//[stack:new obj]
            }
            if (t.IsGenericType)
            {
                var generic = t.GetGenericTypeDefinition();
                if (generic == typeof(Dictionary<,>))
                {
                    Type keyType = t.GetGenericArguments()[0];
                    Type valueType = t.GetGenericArguments()[1];
                    var method = typeof(Obj<>).MakeGenericType(t).GetMethod("m_deep_copy_dict", FInternalStatic).MakeGenericMethod(keyType, valueType);
                    return (Func<T, Dictionary<object, object>, T>)Delegate.CreateDelegate(typeof(Func<T, Dictionary<object, object>, T>), method);
                }
            }
            var dic_t = typeof(Dictionary<object, object>);
            DynamicMethod creator = new DynamicMethod(string.Empty, t, new Type[] { t, dic_t }, t, true);
            ILGenerator il = creator.GetILGenerator();
            var addmethod = typeof(Obj<T>).GetMethod("m_addtodict", FInternalStatic);

            //value type simple return from arg
            if (t.IsSimple())
            {
                il.Emit(OpCodes.Ldarg_S, 0);//[stack: obj]
            }
            else
            {
                Obj.m_create_uninit_generate(il, t);//[stack:new uninited obj]
                LocalBuilder va = il.DeclareLocal(t);
                il.Emit(OpCodes.Stloc_S, va);//[stack:]
                il.Emit(OpCodes.Ldarg_0);//[stack: obj]
                il.Emit(OpCodes.Ldloc_S, va);//[stack: obj,new uninited obj]
                il.Emit(OpCodes.Ldarg_1);//[stack: obj,new uninited obj,dict]
                il.Emit(OpCodes.Call, addmethod);//[stack:]
                var flds = Obj.m_get_flds(t);
                if (flds != null && flds.Length > 0)
                {
                    for (int i = 0; i < flds.Length; ++i)
                    {
                        var fi = flds[i];
                        var ft = fi.FieldType;
                        bool simple = ft.IsSimple();
                        bool box = false;
                        if (t.IsValueType)
                        {
                            il.Emit(OpCodes.Ldloca_S, va);//[stack:addr new uninited obj]
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldloc_S, va);//[stack:new uninited obj]
                        }
                        il.Emit(OpCodes.Ldarg_S, 0);//[stack:new obj
                        il.Emit(OpCodes.Ldfld, fi);//[stack:new obj,fldvalue]
                        if (ft.IsArray)
                        {
                            var ct = m_arr_deepcopy_mi(ft);
                            il.Emit(OpCodes.Ldarg_S, 1); ;//[stack:new obj,fldvalue,dict]
                            il.Emit(OpCodes.Call, ct); ;//[stack:new obj,new fldvalue]
                        }
                        else if (!simple)
                        {
                            box = ft.IsValueType;
                            if (box)
                            {
                                il.Emit(OpCodes.Box, ft);//[stack:new obj,(object)fldvalue]
                            }
                            else
                            {
                                il.Emit(OpCodes.Castclass, typeof(object));//[stack: new obj, (object)fldvalue]
                            }
                            var dc = typeof(Obj).GetMethod("m_deepcopy", FInternalStatic);
                            il.Emit(OpCodes.Ldarg_S, 1);//[stack: new obj, (object)fldvalue,dict]
                            il.Emit(OpCodes.Call, dc);//[stack: new obj, (object)new fldvalue]
                            if (box)
                            {
                                il.Emit(OpCodes.Unbox, ft);//[stack: new obj, new fldvalue]
                            }
                            else
                            {
                                il.Emit(OpCodes.Castclass, ft);//[stack: new obj, new fldvalue]
                            }
                        }
                        il.Emit(OpCodes.Stfld, fi);//[stack:]
                    }
                }
                il.Emit(OpCodes.Ldloc_S, va);//[stack:new obj]
            }
            il.Emit(OpCodes.Ret);
            return (Func<T, Dictionary<object, object>, T>)creator.CreateDelegate(typeof(Func<T, Dictionary<object, object>, T>));
        }

        #endregion Private Methods
    }

    /// <summary>
    /// Create object by type
    /// </summary>
    public class Obj
    {
        #region Internal Fields

        internal static MethodInfo m_get_type_from_handle = typeof(Type).GetMethod("GetTypeFromHandle");
        internal static MethodInfo m_new_uninit_obj = typeof(FormatterServices).GetMethod("GetUninitializedObject");
        internal const BindingFlags FInternalStatic = BindingFlags.Static | BindingFlags.NonPublic;

        #endregion Internal Fields

        #region Private Fields

        private static ConcurrentDictionary<Type, Func<object>> m_map = new ConcurrentDictionary<Type, Func<object>>();
        private static ConcurrentDictionary<Type, Func<object, object>> m_swallow_clone_map = new ConcurrentDictionary<Type, Func<object, object>>();
        private static ConcurrentDictionary<Type, Func<object, Dictionary<object, object>, object>> m_deep_clone_map = new ConcurrentDictionary<Type, Func<object, Dictionary<object, object>, object>>();

        #endregion Private Fields

        #region Public Methods

        /// <summary>
        /// Constructor initialise object create func for type, use constructor without params
        /// </summary>
        public static object New(Type t)
        {
            Func<object> f;
            if (!m_map.TryGetValue(t, out f))
            {
                DynamicMethod creator = new DynamicMethod(string.Empty,
                            typeof(object),
                            new Type[] { },
                            t, true);
                ILGenerator il = creator.GetILGenerator();
                m_create_new_generate(il, t);
                if (t.IsValueType)
                {
                    il.Emit(OpCodes.Box, t);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, typeof(object));
                }
                il.Emit(OpCodes.Ret);
                m_map[t] = (f = (Func<object>)creator.CreateDelegate(typeof(Func<object>)));
            }
            return f();
        }

        /// <summary>
        /// Create new object
        /// </summary>
        /// <typeparam name="T">typeof object</typeparam>
        /// <returns>New object</returns>
        public static T New<T>()
        {
            return Obj<T>.c_tor();
        }

        public static T ShallowCopy<T>(T obj)
        {
            return (T)ShallowCopy((object)obj);
        }

        internal static object m_shallow_copy_fn<T>(object obj)
        {
            return (object)Obj<T>.m_shallowcopy((T)obj);
        }

        public static object ShallowCopy(object obj)
        {
            if (obj == null)
            {
                return obj;
            }
            var ot = obj.GetType();
            Func<object, object> fn;
            if (!m_swallow_clone_map.TryGetValue(ot, out fn))
            {
                var gm = typeof(Obj).GetMethod("m_shallow_copy_fn", FInternalStatic);
                var method = gm.MakeGenericMethod(ot);
                fn = m_swallow_clone_map[ot] = (Func<object, object>)Delegate.CreateDelegate(typeof(Func<object, object>), method);
            }
            return fn(obj);
        }

        public static T DeepCopy<T>(T obj, Dictionary<object, object> dict = null)
        {
            return (T)DeepCopy((object)obj, dict);
        }

        public static object DeepCopy(object obj, Dictionary<object, object> dict)
        {
#if DEBUG
            var dic = dict ?? new Dictionary<object, object>();
            var o = m_deepcopy(obj, dic);
            return o;
#else
            return m_deepcopy(obj, dict ?? new Dictionary<object, object>());
#endif
        }

        #endregion Public Methods

        #region Internal Methods

        internal static object m_deepcopy_fn<T>(object obj, Dictionary<object, object> dict)
        {
            return (object)Obj<T>.m_deepcopy((T)obj, dict);
        }

        internal static object m_deepcopy(object obj, Dictionary<object, object> dic)
        {
            if (obj == null)
                return obj;
            var ot = obj.GetType();
            if (ot.IsSimple())
            {
                return obj;
            }
            object o;
            if (!dic.TryGetValue(obj, out o))
            {
                Func<object, Dictionary<object, object>, object> fn;
                if (!m_deep_clone_map.TryGetValue(ot, out fn))
                {
                    var gm = typeof(Obj).GetMethod("m_deepcopy_fn", FInternalStatic);
                    var method = gm.MakeGenericMethod(ot);
                    fn = m_deep_clone_map[ot] = (Func<object, Dictionary<object, object>, object>)Delegate.CreateDelegate(typeof(Func<object, Dictionary<object, object>, object>), method);
                }
                return dic[obj] = fn(obj, dic);
            }
            return o;
        }

        internal static void m_create_new_generate(ILGenerator il, Type t)
        {
            if (t.IsValueType)
            {
                m_initsruct_generate(il, t);
            }
            else
            {
                var c = t.GetConstructor(new Type[] { });
                il.Emit(OpCodes.Newobj, c);
            }
        }

        internal static void m_create_uninit_generate(ILGenerator il, Type t)
        {
            if (t.IsValueType)
            {
                m_initsruct_generate(il, t);
            }
            else
            {
                /// call FormatterServices.GetUninitializedObject(t)
                il.Emit(OpCodes.Ldtoken, t);
                il.Emit(OpCodes.Call, m_get_type_from_handle);

                //create object
                il.Emit(OpCodes.Call, m_new_uninit_obj);
            }
        }

        internal static FieldInfo[] m_get_flds(Type t)
        {
            return t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); ;
        }

        #endregion Internal Methods

        #region Private Methods

        private static void m_initsruct_generate(ILGenerator il, Type t)
        {
            var vt = il.DeclareLocal(t);
            il.Emit(OpCodes.Ldloca_S, vt);
            il.Emit(OpCodes.Initobj, t);
            il.Emit(OpCodes.Ldloc_S, vt);
        }

        #endregion Private Methods
    }
}