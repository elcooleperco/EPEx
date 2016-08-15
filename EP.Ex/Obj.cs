using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;

namespace EP.Ex
{
    /// <summary>
    /// Class implement object extension
    /// </summary>
    /// <typeparam name="T">Object</typeparam>
    public class Obj<T>// where T : class
    {
        internal static Func<T> c_tor;
        internal static Func<T, T> m_shallowcopy;
        /// <summary>
        /// Constructor initialise object create func, use constructor without params
        /// </summary>
        static Obj()
        {
            c_tor = m_c_tor_func();
            m_shallowcopy = m_shallowcopy_func();
        }
        static Func<T> m_c_tor_func()
        {
            var t = typeof(T);

            DynamicMethod creator = new DynamicMethod(string.Empty,
                        t,
                        new Type[] { },
                        t, true);
            ILGenerator il = creator.GetILGenerator();
            Obj.m_create_new_generate(il, t);
            il.Emit(OpCodes.Ret);
            return (Func<T>)creator.CreateDelegate(typeof(Func<T>));
        }
        static Func<T, T> m_shallowcopy_func()
        {
            var t = typeof(T);
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
    }
    /// <summary>
    /// Create object by type
    /// </summary>
    public class Obj
    {
        static ConcurrentDictionary<Type, Func<object>> m_map = new ConcurrentDictionary<Type, Func<object>>();
        static ConcurrentDictionary<Type, Func<object, object>> m_swallow_clone_map = new ConcurrentDictionary<Type, Func<object, object>>();
        internal static MethodInfo m_new_uninit_obj = typeof(FormatterServices).GetMethod("GetUninitializedObject");
        internal static MethodInfo m_get_type_from_handle = typeof(Type).GetMethod("GetTypeFromHandle");
        static void m_initsruct_generate(ILGenerator il, Type t)
        {
            var vt = il.DeclareLocal(t);
            il.Emit(OpCodes.Ldloca_S, vt);
            il.Emit(OpCodes.Initobj, t);
            il.Emit(OpCodes.Ldloc_S, vt);
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
        internal static FieldInfo[] m_get_flds(Type t)
        {
            return t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); ;
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
            return Obj<T>.m_shallowcopy(obj);
        }
    }
}
