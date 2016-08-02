using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
        /// <summary>
        /// Constructor initialise object create func, use constructor without params
        /// </summary>
        static Obj()
        {
            var t = typeof(T);

            DynamicMethod creator = new DynamicMethod(string.Empty,
                        t,
                        new Type[] { },
                        t, true);
            ILGenerator il = creator.GetILGenerator();
            Obj.m_create_new_generate(il, t);
            il.Emit(OpCodes.Ret);
            c_tor = (Func<T>)creator.CreateDelegate(typeof(Func<T>));

        }

        /// <summary>
        /// Create new object
        /// </summary>
        /// <returns>new object type T</returns>
        public static T New()
        {
            return c_tor();
        }
    }
    /// <summary>
    /// Create object by type
    /// </summary>
    public class Obj
    {
        static ConcurrentDictionary<Type, Func<object>> m_map = new ConcurrentDictionary<Type, Func<object>>();
        internal static void m_create_new_generate(ILGenerator il, Type t)
        {
            if (t.IsValueType)
            {
                var vt = il.DeclareLocal(t);
                il.Emit(OpCodes.Ldloca_S, vt);
                il.Emit(OpCodes.Initobj, t);
                il.Emit(OpCodes.Ldloc_S, vt);
            }
            else
            {
                var c = t.GetConstructor(new Type[] { });
                il.Emit(OpCodes.Newobj, c);
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
    }
}
