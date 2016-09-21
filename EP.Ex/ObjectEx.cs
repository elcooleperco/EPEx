using EP.Ex;
using System.Collections.Generic;

namespace System
{
    public static class ObjectEx
    {
        #region Public Methods

        public static T DeepCopy<T>(this T obj, Dictionary<object, object> dict = null)
        {
            return Obj.DeepCopy<T>(obj, dict);
        }

        public static T ShallowCopy<T>(this T obj)
        {
            return Obj.ShallowCopy<T>(obj);
        }

        #endregion Public Methods
    }
}