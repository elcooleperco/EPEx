using System.Collections.Generic;

namespace EP.Ex
{
    public sealed class ReferenceComparer : ReferenceComparer<object>
    {
    }

    public class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        #region Private Fields

        private static ReferenceComparer<T> m_instance;

        #endregion Private Fields

        #region Public Properties

        public static ReferenceComparer<T> Instance
        {
            get
            {
                return m_instance ?? (m_instance = new ReferenceComparer<T>());
            }
        }

        #endregion Public Properties

        #region Public Methods

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        #endregion Public Methods
    }
}