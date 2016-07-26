using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace EP.Ex
{
    /// <summary>
    /// Class implement object extension
    /// </summary>
    /// <typeparam name="T">Object</typeparam>
    public class Obj<T> where T : class
    {
        static Func<T> c_tor;
        /// <summary>
        /// Constructor initialise object create func, use constructor without params
        /// </summary>
        static Obj()
        {
            var t = typeof(T);
            var c = t.GetConstructor(new Type[] { });
            DynamicMethod creator = new DynamicMethod(string.Empty,
                        t,
                        new Type[] { },
                        t, true);
            ILGenerator il = creator.GetILGenerator();

            il.Emit(OpCodes.Newobj, c);
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
        static ConcurrentDictionary<Type, Func<object>> map = new ConcurrentDictionary<Type, Func<object>>();
        /// <summary>
        /// Constructor initialise object create func for type, use constructor without params
        /// </summary>
        public static object New(Type t)
        {
            Func<object> f;
            if (!map.TryGetValue(t, out f))
            {
                var c = t.GetConstructor(new Type[] { });
                DynamicMethod creator = new DynamicMethod(string.Empty,
                            t,
                            new Type[] { },
                            t);
                ILGenerator il = creator.GetILGenerator();

                il.Emit(OpCodes.Newobj, c);
                il.Emit(OpCodes.Castclass, typeof(object));
                il.Emit(OpCodes.Ret);
                map[t] = (f = (Func<object>)creator.CreateDelegate(typeof(Func<object>)));
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
            return (T)New(typeof(T));
        }
    }
}
