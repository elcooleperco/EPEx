using Microsoft.VisualStudio.TestTools.UnitTesting;
using EP.Ex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EP.Ex.Tests
{
    [TestClass()]
    public class ObjTests
    {
        public class TestClass
        {
            public string Str;
            public TestClass()
            {
                Str = "Test Str";
            }
        }
        [TestMethod()]
        public void NewTest()
        {
            TestClass m = (TestClass)Obj.New(typeof(TestClass));
            TestClass c = (TestClass)Obj.New<TestClass>();
            TestClass t = Obj<TestClass>.New();
            Assert.IsNotNull(m);
            Assert.IsNotNull(t);
            Assert.IsNotNull(c);
            Assert.IsInstanceOfType(m, typeof(TestClass));
            Assert.IsInstanceOfType(c, typeof(TestClass));
            Assert.IsInstanceOfType(t, typeof(TestClass));
        }
    }
}