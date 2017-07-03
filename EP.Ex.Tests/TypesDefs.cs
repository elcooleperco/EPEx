using System;
using System.Collections.Generic;

namespace EP.Ex.Tests
{
    public struct st1
    {
        #region Public Fields

        public string s;

        #endregion Public Fields
    }

    public class A
    {
        #region Public Properties

        public A other { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public A()
        {
        }

        #endregion Public Constructors
    }

    public class B : A
    {
        #region Public Constructors

        public B()
            : base()
        {
        }

        #endregion Public Constructors
    }

    public class DeepTestClass
    {
        #region Public Fields

        public int[] Arr;
        public Dictionary<string, object> Dict = new Dictionary<string, object>();
        public string Str;

        #endregion Public Fields

        #region Public Constructors

        public DeepTestClass Linked;

        public DeepTestClass()
        {
            Str = "Test Str";
            Dict["Me"] = this;
            Arr = new int[4] { 1, 234, 432, 1 };
        }

        #endregion Public Constructors
    }

    public class TestClass
    {
        #region Public Fields

        public string Str;

        #endregion Public Fields

        #region Public Constructors

        public TestClass()
        {
            Str = "Test Str";
        }

        #endregion Public Constructors
    }

    public class TestNode
    {
        #region Public Properties

        public DateTime fld { get; set; }

        #endregion Public Properties
    }
}