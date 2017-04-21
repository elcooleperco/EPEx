using System.Collections.Generic;

namespace EP.Ex
{
    /// <summary>
    /// Reference compare of object
    /// </summary>
    public sealed class ReferenceComparer : ReferenceComparer<object>
    {
    }

    /// <summary>
    /// Reference comparer of objects
    /// </summary>
    /// <typeparam name="T">object type</typeparam>
    public class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        #region Private Fields

        /// <summary>
        /// Comparern instance
        /// </summary>
        private static ReferenceComparer<T> m_instance;

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Get comparer instance
        /// </summary>
        public static ReferenceComparer<T> Instance
        {
            get
            {
                return m_instance ?? (m_instance = new ReferenceComparer<T>());
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Compare reference of two object
        /// </summary>
        /// <param name="x">first object</param>
        /// <param name="y">second object</param>
        /// <returns></returns>
        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        /// <summary>
        /// Get HASH code
        /// </summary>
        /// <param name="obj">Source Object</param>
        /// <returns>Hash code of source object</returns>
        public int GetHashCode(T obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        #endregion Public Methods
    }
}