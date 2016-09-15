using EP.Ex;

namespace System
{
    public static class ObjectEx
    {
        #region Public Methods

        public static T DeepCopy<T>(this T obj)
        {
            return Obj.DeepCopy<T>(obj);
        }

        public static T ShallowCopy<T>(this T obj)
        {
            return Obj.ShallowCopy<T>(obj);
        }

        #endregion Public Methods
    }
}