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

        internal const BindingFlags FInternalStatic = Obj.FInternalStatic;
        internal static Func<T> c_tor;
        internal static Func<T, Dictionary<object, object>, T> m_deepcopy;
        internal static Func<T, T> m_shallowcopy;

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
        /// Create new, object using constructor without arguments
        /// </summary>
        /// <returns>new object type T, if constructor w/o args absent missing than null.</returns>
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

        private static MethodInfo m_add_to_dict_method = typeof(Obj<T>).GetMethod(nameof(Obj<T>.m_addtodict), FInternalStatic);

        /// <summary>
        /// Override default Deep Copy function
        /// </summary>
        /// <param name="fn">Overrided clone function</param>
        public static void SetDeepCopyFn(Func<T, Dictionary<object, object>, T> fn)
        {
            m_deepcopy = fn;
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
        /// Add instruction to ilGenerator that create new array same rank and length that
        /// source(arg[0]), no stack modification at the end.
        /// </summary>
        /// <param name="il">instruction creator</param>
        /// <param name="dst">variable hold destination array</param>
        /// <param name="dim">variables array that holds dimention length</param>
        private static void m_arr_create_il_inst(ILGenerator il, out LocalBuilder dst, out LocalBuilder[] dim)
        {
            var t = typeof(T);
            var arrt = t.GetElementType();
            var rank = t.GetArrayRank();
            dim = new LocalBuilder[rank];
            Type[] tis = new Type[rank];
            dst = il.DeclareLocal(t);

            for (int i = 0; i < rank; ++i)
            {
                dim[i] = il.DeclareLocal(typeof(int));
                tis[i] = typeof(Int32);
            }

            var ctor = t.GetConstructor(tis);
            var glen = typeof(Array).GetMethod("GetLength", BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < rank; ++i)
            {
                il.Emit(OpCodes.Ldarg_0);//load argument
                il.Emit(OpCodes.Ldc_I4, i);

                //stack[array,rank]
                il.Emit(OpCodes.Call, glen);

                //remove prev [array,rank] append integer(value length on current rank(dim))
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc_S, dim[i]);
            }

            il.Emit(OpCodes.Newobj, ctor);//create Array same rank
            il.Emit(OpCodes.Stloc_S, dst);
        }

        /// <summary>
        /// Get delegate that create deep copy of multidimentional array
        /// </summary>
        /// <param name="t">generic type</param>
        /// <returns>Delegate</returns>
        private static Func<T, Dictionary<object, object>, T> m_arr_deep_copy_mi()
        {
            var t = typeof(T);
            var arrt = t.GetElementType();
            var rank = t.GetArrayRank();
            bool simple = t.IsSimple();
            Type[] tis = new Type[rank];
            LocalBuilder[] dim;
            LocalBuilder[] loop_var = new LocalBuilder[rank];
            Label[] start_loop = new Label[rank];
            Label[] next_loop = new Label[rank];
            LocalBuilder dst;
            DynamicMethod creator = new DynamicMethod(string.Empty, t, new Type[] { t, typeof(Dictionary<object, object>) }, typeof(Obj), true);

            ILGenerator il = creator.GetILGenerator();
            LocalBuilder cur = il.DeclareLocal(t);
            for (int i = 0; i < rank; ++i)
            {
                loop_var[i] = il.DeclareLocal(typeof(int));
            }
            m_arr_create_il_inst(il, out dst, out dim);

            //store new array at the walked dictionary
            il.Emit(OpCodes.Ldarg_0);//[stack: src]
            il.Emit(OpCodes.Ldloc_S, dst);//[stack: src,new dst]
            il.Emit(OpCodes.Ldarg_1);//[stack: src,new dst,dict]
            il.Emit(OpCodes.Call, m_add_to_dict_method);//[stack:]
            var gv = t.GetMethod("Get", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var sv = t.GetMethod("Set", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            //start for(int i=0;i<dim[i];++i)
            for (int i = 0; i < rank; ++i)
            {
                start_loop[i] = il.DefineLabel();
                next_loop[i] = il.DefineLabel();
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc_S, loop_var[i]);
                il.Emit(OpCodes.Br, start_loop[i]);
                il.MarkLabel(next_loop[i]);
            }

            //loop body
            //get item from src array
            il.Emit(OpCodes.Ldarg_0);//stack [src array]
            for (int i = 0; i < rank; ++i)
            {
                il.Emit(OpCodes.Ldloc_S, loop_var[i]);//stack ...,[src array],loop_var[1],...,loop_var[i]
            }
            il.Emit(OpCodes.Call, gv);//stack ...,[element from array]
            il.Emit(OpCodes.Stloc_S, cur);

            if (!simple)
            {
                il.Emit(OpCodes.Ldloc_S, cur);
                m_deep_clone_obj_il_gen(il, arrt);
                il.Emit(OpCodes.Stloc_S, cur);
            }

            //set item to dst array
            il.Emit(OpCodes.Ldloc_S, dst);//stack [dst array]
            for (int i = 0; i < rank; ++i)
            {
                il.Emit(OpCodes.Ldloc_S, loop_var[i]);//stack ...,[dst array],loop_var[1],...,loop_var[i]
            }
            il.Emit(OpCodes.Ldloc_S, cur);//stack ...,[dst array],loop_var[1],...,loop_var[rank],[element copy]
            il.Emit(OpCodes.Call, sv);

            //end loop
            for (int i = rank - 1; i >= 0; --i)
            {
                //++i
                il.Emit(OpCodes.Ldloc_S, loop_var[i]);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc_S, loop_var[i]);

                //check i<dim[i]
                il.MarkLabel(start_loop[i]);
                il.Emit(OpCodes.Ldloc_S, loop_var[i]);
                il.Emit(OpCodes.Ldloc_S, dim[i]);
                il.Emit(OpCodes.Clt);
                il.Emit(OpCodes.Brtrue, next_loop[i]);
            }
            il.Emit(OpCodes.Ldloc_S, dst.LocalIndex);
            il.Emit(OpCodes.Ret);

            return (Func<T, Dictionary<object, object>, T>)creator.CreateDelegate(typeof(Func<T, Dictionary<object, object>, T>));
        }

        /// <summary>
        /// Get delegate that create shallow copy of array
        /// </summary>
        /// <param name="t">generic type</param>
        /// <returns>Array shallow copy delegate</returns>
        private static Func<T, T> m_arr_shallow_copy_mi()
        {
            var t = typeof(T);
            var arrt = t.GetElementType();
            var rank = t.GetArrayRank();
            Type[] tis = new Type[rank];
            LocalBuilder[] dim;
            LocalBuilder dst;
            DynamicMethod creator = new DynamicMethod(string.Empty, t, new Type[] { t }, typeof(Obj), true);

            ILGenerator il = creator.GetILGenerator();
            m_arr_create_il_inst(il, out dst, out dim);

            var ca = typeof(Array).GetMethod("Copy", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Array), typeof(Array), typeof(int) }, null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc_S, dst);

            //mul all ranks
            for (int i = 0; i < rank; ++i)
            {
                il.Emit(OpCodes.Ldloc_S, dim[i]);
                if (i > 0) il.Emit(OpCodes.Mul);
            }

            //call Array.Copy(args[0],dst,rank1*rank2*...*rankN)
            il.Emit(OpCodes.Call, ca);//return void

            il.Emit(OpCodes.Ldloc_S, dst);
            il.Emit(OpCodes.Ret);

            return (Func<T, T>)creator.CreateDelegate(typeof(Func<T, T>));
        }

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
        /// generate instruction that take obj from stack and make deep copy
        /// </summary>
        /// <param name="il">instruction creator</param>
        /// <param name="objtype">type of copied object</param>
        private static void m_deep_clone_obj_il_gen(ILGenerator il, Type objtype)
        {
            bool box = objtype.IsValueType;

            if (box)
            {
                il.Emit(OpCodes.Box, objtype);//[(object)obj]
            }
            //else
            //{
            //    il.Emit(OpCodes.Castclass, typeof(object));//[(object)obj]
            //}
            var dc = typeof(Obj).GetMethod(nameof(Obj.m_deepcopy), FInternalStatic);
            il.Emit(OpCodes.Ldarg_1);//[(object)obj,dict]
            il.Emit(OpCodes.Call, dc);//[(object)new obj]
            if (box)
            {
                il.Emit(OpCodes.Unbox, objtype);//[new obj]
            }
            //else
            //{
            //    il.Emit(OpCodes.Castclass, objtype);//[new obj]
            //}
        }

        /// <summary>
        /// Get default deep copy function
        /// </summary>
        /// <returns>delegate that create deep copy</returns>
        private static Func<T, Dictionary<object, object>, T> m_deepcopy_func()
        {
            var t = typeof(T);

            var o = CopyBaseHelper.DeepCopyFunc<T>();
            if (o != null)
            {
                return o;
            }
            else if (t.IsArray)
            {
                return m_arr_deep_copy_mi();
            }
            var dic_t = typeof(Dictionary<object, object>);
            DynamicMethod creator = new DynamicMethod(string.Empty, t, new Type[] { t, dic_t }, t, true);
            ILGenerator il = creator.GetILGenerator();

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
                il.Emit(OpCodes.Call, m_add_to_dict_method);//[stack:]
                foreach (var fi in Obj.m_get_flds_each(t))
                {
                    var ft = fi.FieldType;
#if DEBUG
                    il.EmitWriteLine($"Copy fld: {fi.Name}, of type {fi.FieldType}");
#endif
                    bool simple = ft.IsSimple();
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

                    if (!simple)
                    {
                        m_deep_clone_obj_il_gen(il, ft);//[stack:new obj,new fldvalue]
                    }

                    il.Emit(OpCodes.Stfld, fi);//[stack: new obj]
                }
                il.Emit(OpCodes.Ldloc_S, va);//[stack:new obj]
            }
#if DEBUG
            il.EmitWriteLine("*********");
#endif
            il.Emit(OpCodes.Ret);
            return (Func<T, Dictionary<object, object>, T>)creator.CreateDelegate(typeof(Func<T, Dictionary<object, object>, T>));
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
                return m_arr_shallow_copy_mi();
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
                foreach (var fi in Obj.m_get_flds_each(t))
                {
                    il.Emit(OpCodes.Ldloc_S, va);
                    il.Emit(OpCodes.Ldarg_S, 0);
                    il.Emit(OpCodes.Ldfld, fi);
                    il.Emit(OpCodes.Stfld, fi);
                }
                il.Emit(OpCodes.Ldloc_S, va);
            }
            il.Emit(OpCodes.Ret);
            return (Func<T, T>)creator.CreateDelegate(typeof(Func<T, T>));
        }

        #endregion Private Methods
    }

    /// <summary>
    /// Create object by type
    /// </summary>
    public class Obj
    {
        #region Internal Fields

        internal const BindingFlags FInternalStatic = BindingFlags.Static | BindingFlags.NonPublic;
        internal const BindingFlags FPublicStatic = BindingFlags.Static | BindingFlags.Public;
        internal static MethodInfo m_get_type_from_handle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));
        internal static MethodInfo m_new_uninit_obj = typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject));

        #endregion Internal Fields

        #region Private Fields

        private static ConcurrentDictionary<Type, Func<object, Dictionary<object, object>, object>> m_deep_clone_map = new ConcurrentDictionary<Type, Func<object, Dictionary<object, object>, object>>();
        private static ConcurrentDictionary<Type, Func<object>> m_map = new ConcurrentDictionary<Type, Func<object>>();
        private static ConcurrentDictionary<Type, Func<object, object>> m_swallow_clone_map = new ConcurrentDictionary<Type, Func<object, object>>();

        #endregion Private Fields

        #region Public Methods

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
                //else
                //{
                //    il.Emit(OpCodes.Castclass, typeof(object));
                //}
                il.Emit(OpCodes.Ret);
                m_map[t] = (f = (Func<object>)creator.CreateDelegate(typeof(Func<object>)));
            }
            return f();
        }

        /// <summary>
        /// Create new object, using constructor without arguments
        /// </summary>
        /// <typeparam name="T">typeof object</typeparam>
        /// <returns>New object, if constructor w/o args absent missing than null</returns>
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

        internal static object m_shallow_copy_fn<T>(object obj)
        {
            return (object)Obj<T>.m_shallowcopy((T)obj);
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary> Set instruction to create new object, or return null if absent constructor without args </summary> 
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
                //if constructor without argument absent than return null
                if (c != null)
                {
                    il.Emit(OpCodes.Newobj, c);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }
            }
        }

        /// <summary> Set instruction to create new uninitialized object </summary> 
        /// <param name="il">MSIL instruction generator<param> 
        /// <param name="t">type of new object</param
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
        /// Get list instance fields
        /// </summary>
        /// <param name="t">type of object</param>
        /// <returns></returns>
        internal static FieldInfo[] m_get_flds(Type t)
        {
            return t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly); ;
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary> Set instruction to init new structure </summary> 
        /// <param name="il">MSIL instruction generator<param> 
        /// <param name="t">type of new struct</param>
        private static void m_initsruct_generate(ILGenerator il, Type t)
        {
            var vt = il.DeclareLocal(t);
            il.Emit(OpCodes.Ldloca_S, vt);
            il.Emit(OpCodes.Initobj, t);
            il.Emit(OpCodes.Ldloc_S, vt);
        }
        /// <summary>
        /// Get all private,internal,public filds include inherited
        /// </summary>
        /// <param name="t">type of object/struct</param>
        /// <returns>Fields enumerator</returns>
        internal static IEnumerable<FieldInfo> m_get_flds_each(Type t)
        {
            //if (t.IsValueType) return m_get_flds(t);

            while (t != typeof(object))
            {
                var flds = m_get_flds(t);
                for (var i = 0; i < flds.Length; ++i) yield return flds[i];
                if (t.IsValueType) yield break;
                t = t.BaseType;
            }
        }

        #endregion Private Methods
    }
}