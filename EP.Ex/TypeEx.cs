namespace System
{
    /// <summary>
    /// Type extension
    /// </summary>
    public static class TypeEx
    {
        #region Public Methods

        /// <summary>
        /// Check is type is simple: Primitive,Enum, string, decimal
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>True - if type is simple</returns>
        public static bool IsSimple(this Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }

        #endregion Public Methods
    }
}