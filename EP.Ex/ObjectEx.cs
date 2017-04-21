using EP.Ex;
using System.Collections.Generic;

namespace System
{
    public static class ObjectEx
    {
        #region Public Methods

        /// <summary>
        /// Create deep copy of object
        /// </summary>
        /// <typeparam name="T">Type of new object</typeparam>
        /// <param name="obj">Source object</param>
        /// <param name="dict">
        /// Dictionary, that accumulate copies of objects, to have one object - one copy
        /// </param>
        /// <returns>Deep copy of object</returns>
        public static T DeepCopy<T>(this T obj, Dictionary<object, object> dict = null)
        {
            return Obj.DeepCopy<T>(obj, dict);
        }

        /// <summary>
        /// Create shallow copy of object
        /// </summary>
        /// <typeparam name="T">Type of new object</typeparam>
        /// <param name="obj">Source object</param>
        /// <returns>Shallow copy of object</returns>
        /// ///
        /// <returns></returns>
        public static T ShallowCopy<T>(this T obj)
        {
            return Obj.ShallowCopy<T>(obj);
        }

        #endregion Public Methods
    }
}