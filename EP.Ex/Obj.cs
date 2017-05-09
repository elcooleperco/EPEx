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

        /// <summary>
        /// Create shallow copy of object
        /// </summary>
        /// <param name="obj">Initial object</param>
        /// <returns>Shallow copy of object</returns>
        public static T ShallowCopy(T obj)
        {
            return m_shallowcopy(obj);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Get constructor of object
        /// </summary>
        /// <returns>Return cunstructor as delegate, without parameters</returns>
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

        /// <summary>
        /// Get delegate function that create shallow copy of object
        /// </summary>
        /// <returns>Delegate</returns>
        private static Func<T, T> m_shallowcopy_func()
        {
            var t = typeof(T);
            if (t.IsArray)
            {
                return m_arr_copy_mi();
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

        /// <summary>
        /// Create copy of single dimention Array
        /// </summary>
        /// <param name="src">Source array</param>
        /// <returns>Copy of array</returns>
        private static T[] m_copy_array_1d(T[] src)
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
        /// <summary>
        /// Add element to dictionary
        /// </summary>
        /// <param name="oldobj">key object</param>
        /// <param name="newobj">value object</param>
        /// <param name="dic">target dictionary</param>
        private static void m_addtodict(T oldobj, T newobj, Dictionary<object, object> dic)
        {
            dic[oldobj] = newobj;

            //return newobj;
        }

        /// <summary>
        /// Get method info of array copym_copy_array
        /// </summary>
        /// <param name="t">generic type</param>
        /// <returns>Method info</returns>
        private static Func<T, T> m_arr_copy_mi()
        {
            var t = typeof(T);
            var arrt = t.GetElementType();
            var rank = t.GetArrayRank();
            if (rank == 1)
            {
                var mi = typeof(Obj<>).MakeGenericType(arrt).GetMethod(nameof(Obj<object>.m_copy_array_1d), FInternalStatic);
                return (Func<T, T>)Delegate.CreateDelegate(typeof(Func<T, T>), mi);
            }
            else
            {
                Type[] tis = new Type[rank];
                LocalBuilder[] dim = new LocalBuilder[rank];

                DynamicMethod creator = new DynamicMethod(string.Empty, t, new Type[] { t }, t, true);

                ILGenerator il = creator.GetILGenerator();
                var dst = il.DeclareLocal(t);


                for (int i = 0; i < rank; ++i)
                {
                    dim[i] = il.DeclareLocal(typeof(int));
                    tis[i] = typeof(Int32);
                }

                var ctor = t.GetConstructor(tis);
                var glen = typeof(Array).GetMethod("GetLength", BindingFlags.Public | BindingFlags.Instance);
                var gub = typeof(Array).GetMethod("GetUpperBound", BindingFlags.Public | BindingFlags.Instance);
                var gv = typeof(Array).GetMethod("Get", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var sv = typeof(Array).GetMethod("Set", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                for (int i = 0; i < rank; ++i)
                {
                    il.Emit(OpCodes.Ldarg_0);//load argument
                    il.Emit(OpCodes.Ldc_I4,i);
                    //stack[array,rank]
                    il.Emit(OpCodes.Call, glen);
                    //remove prev [array,rank] append integer(value length on current rank(dim))
                }
                il.Emit(OpCodes.Call, ctor);//create Array same rank
                il.Emit(OpCodes.Stloc, dst.LocalIndex);
            }
        }

        /// <summary>
        /// Override default Deep Copy function
        /// </summary>
        /// <param name="fn">Overrided clone function</param>
        public static void SetDeepCopyFn(Func<T, Dictionary<object, object>, T> fn)
        {
            m_deepcopy = fn;
        }

        /// <summary>
        /// Get default deep copy function
        /// </summary>
        /// <returns>delegate that create deep copy</returns>
        private static Func<T, Dictionary<object, object>, T> m_deepcopy_func()
        {
            var t = typeof(T);

            //if (t.IsArray)
            //{
            //    var method = m_arr_deepcopy_mi(t);
            //    return (Func<T, Dictionary<object, object>, T>)Delegate.CreateDelegate(typeof(Func<T, Dictionary<object, object>, T>), method);

            //    //il.Emit(OpCodes.Ldarg_0);//[stack:obj]
            //    //il.Emit(OpCodes.Ldarg_1);//[stack:obj,dict]
            //    //il.Emit(OpCodes.Call, method);//[stack:new obj]
            //}
            var o = CopyBaseHelper.DeepCopyFunc<T>();
            if (o != null)
            {
                return o;
            }
            var dic_t = typeof(Dictionary<object, object>);
            DynamicMethod creator = new DynamicMethod(string.Empty, t, new Type[] { t, dic_t }, t, true);
            ILGenerator il = creator.GetILGenerator();
            var addmethod = typeof(Obj<T>).GetMethod(nameof(Obj<T>.m_addtodict), FInternalStatic);

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

                        //if (ft.IsArray)
                        //{
                        //    var ct = m_arr_deepcopy_mi(ft);
                        //    il.Emit(OpCodes.Ldarg_S, 1); ;//[stack:new obj,fldvalue,dict]
                        //    il.Emit(OpCodes.Call, ct); ;//[stack:new obj,new fldvalue]
                        //}
                        if (!simple)
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
                            var dc = typeof(Obj).GetMethod(nameof(Obj.m_deepcopy), FInternalStatic);
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

        internal static MethodInfo m_get_type_from_handle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));
        internal static MethodInfo m_new_uninit_obj = typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject));
        internal const BindingFlags FInternalStatic = BindingFlags.Static | BindingFlags.NonPublic;
        internal const BindingFlags FPublicStatic = BindingFlags.Static | BindingFlags.Public;

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

        /// <summary>
        /// Create shallow copy of object
        /// </summary>
        /// <typeparam name="T">Copied object type</typeparam>
        /// <param name="obj">Source Object</param>
        /// <returns></returns>
        public static T ShallowCopy<T>(T obj)
        {
            return (T)ShallowCopy((object)obj);
        }

        internal static object m_shallow_copy_fn<T>(object obj)
        {
            return (object)Obj<T>.m_shallowcopy((T)obj);
        }

        /// <summary>
        /// Create shallow copy of object
        /// </summary>
        /// <param name="obj">source object</param>
        /// <returns>Object copy</returns>
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
                var gm = typeof(Obj).GetMethod(nameof(Obj.m_shallow_copy_fn), FInternalStatic);
                var method = gm.MakeGenericMethod(ot);
                fn = m_swallow_clone_map[ot] = (Func<object, object>)Delegate.CreateDelegate(typeof(Func<object, object>), method);
            }
            return fn(obj);
        }

        /// <summary>
        /// Create Deep copy of object
        /// </summary>
        /// <typeparam name="T">Type of copying object</typeparam>
        /// <param name="obj">Source object</param>
        /// <param name="dict">
        /// Dictionary, that accumulate copies of objects, to have one object - one copy
        /// </param>
        /// <returns>Copy of object</returns>
        public static T DeepCopy<T>(T obj, Dictionary<object, object> dict = null)
        {
            return (T)DeepCopy((object)obj, dict);
        }

        /// <summary>
        /// Create Deep copy of object
        /// </summary>
        /// <param name="obj">Source object</param>
        /// <param name="dict">
        /// Dictionary, that accumulate copies of objects, to have one object - one copy
        /// </param>
        /// <returns>Copy of object</returns>
        public static object DeepCopy(object obj, Dictionary<object, object> dict)
        {
            var dic = dict ?? new Dictionary<object, object>(ReferenceComparer.Instance);
#if DEBUG
            var o = m_deepcopy(obj, dic);
            return o;
#else
            return m_deepcopy(obj, dic);
#endif
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// Get deep copy of object
        /// </summary>
        /// <typeparam name="T">Type of copying object</typeparam>
        /// <param name="obj">Copying object</param>
        /// <param name="dict">
        /// Dictionary, that accumulate copies of objects, to have one object - one copy
        /// </param>
        /// <returns>Copy of object</returns>
        internal static object m_deepcopy_fn<T>(object obj, Dictionary<object, object> dict)
        {
            return (object)Obj<T>.m_deepcopy((T)obj, dict);
        }

        /// <summary>
        /// Get deep copy of object
        /// </summary>
        /// <param name="obj">Copying object</param>
        /// <param name="dic">
        /// Dictionary, that accumulate copies of objects, to have one object - one copy
        /// </param>
        /// <returns>Copy of object</returns>
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
                    var gm = typeof(Obj).GetMethod(nameof(Obj.m_deepcopy_fn), FInternalStatic);
                    var method = gm.MakeGenericMethod(ot);
                    fn = m_deep_clone_map[ot] = (Func<object, Dictionary<object, object>, object>)
                        Delegate.CreateDelegate(typeof(Func<object, Dictionary<object, object>, object>), method);
                }
                return dic[obj] = fn(obj, dic);
            }
            return o;
        }

        /// <summary> Set instruction to create new object </summary> 
        /// <param name="il">MSIL instruction generator<param> 
        /// <param name="t">type of new object</param>
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

        /// <summary> Set instruction to create new uninitialized object </summary> <param
        /// name="il">MSIL instruction generator<param> <param name="t">type of new object</param
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
        /// Get list instance fields
        /// </summary>
        /// <param name="t">type of object</param>
        /// <returns></returns>
        internal static FieldInfo[] m_get_flds(Type t)
        {
            return t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); ;
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary> Set instruction to init new structure </summary> <param name="il">MSIL
        /// instruction generator<param> <param name="t">type of new struct</param
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