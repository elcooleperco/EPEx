using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace EP.Ex.Tests
{
    [TestClass()]
    public class ObjTests
    {
        #region Public Structs

        public struct st1
        {
            #region Public Fields

            public string s;

            #endregion Public Fields
        }

        #endregion Public Structs

        #region Public Classes

        public class DeepTestClass
        {
            #region Public Fields

            public Dictionary<string, object> Dict = new Dictionary<string, object>();
            public string Str;

            #endregion Public Fields

            #region Public Constructors

            public DeepTestClass Linked;

            public DeepTestClass()
            {
                Str = "Test Str";
                Dict["Me"] = this;
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

        #endregion Public Classes

        #region Public Methods

        [TestMethod()]
        public void NewPerformanceTest()
        {
            const int count = 1000000;
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < count; ++i)
            {
                var obj = new TestClass();
            }
            stopwatch.Stop();
            Console.WriteLine("new object: {0} ms", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
            for (int i = 0; i < count; ++i)
            {
                var obj = Obj<TestClass>.New();
            }
            stopwatch.Stop();
            Console.WriteLine("Obj<TestClass>.New : {0} ms", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
            for (int i = 0; i < count; ++i)
            {
                var obj = Obj.New<TestClass>();
            }
            stopwatch.Stop();
            Console.WriteLine("Obj.New<TestClass> : {0} ms", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
            for (int i = 0; i < count; ++i)
            {
                var obj = Obj.New(typeof(TestClass));
            }
            stopwatch.Stop();
            Console.WriteLine("Obj.New typeof(TestClass) : {0} ms", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
            for (int i = 0; i < count; ++i)
            {
                var obj = Activator.CreateInstance<TestClass>();
            }
            stopwatch.Stop();
            Console.WriteLine("Activator<TestClass> : {0} ms", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
            for (int i = 0; i < count; ++i)
            {
                var obj = Activator.CreateInstance(typeof(TestClass));
            }
            stopwatch.Stop();
            Console.WriteLine("Activator.CreateInstance(typeof(TestClass)) : {0} ms", stopwatch.ElapsedMilliseconds);
        }

        [TestMethod()]
        public void NewTest()
        {
            const string str = "231231";
            TestClass m = (TestClass)Obj.New(typeof(TestClass));
            TestClass c = (TestClass)Obj.New<TestClass>();
            TestClass k = (TestClass)Obj.New(typeof(TestClass));
            TestClass t = Obj<TestClass>.New();
            st1 s = Obj<st1>.New();
            st1 s1 = (st1)Obj.New<st1>();
            st1 s2 = (st1)Obj.New(typeof(st1));

            Console.WriteLine(s2.s);
            s2.s = str;
            Console.WriteLine(s2.s);
            Assert.IsNotNull(m);
            Assert.IsNotNull(t);
            Assert.IsNotNull(c);
            Assert.IsInstanceOfType(m, typeof(TestClass));
            Assert.IsInstanceOfType(c, typeof(TestClass));
            Assert.IsInstanceOfType(t, typeof(TestClass));
            Assert.IsInstanceOfType(s, typeof(st1));
            Assert.IsInstanceOfType(s1, typeof(st1));
            Assert.IsInstanceOfType(s2, typeof(st1));
            Assert.AreEqual(s2.s, str);
        }

        [TestMethod()]
        public void NewVsDeepCopyPerformanceTest()
        {
            const int count = 10000;
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < count; ++i)
            {
                var obj = new DeepTestClass();
            }
            stopwatch.Stop();
            Console.WriteLine("new object: {0} ms", stopwatch.ElapsedMilliseconds);
            var co = new DeepTestClass();
            var p = co.DeepCopy();
            stopwatch.Restart();
            for (int i = 0; i < count; ++i)
            {
                var obj = Obj.DeepCopy(co);
            }
            stopwatch.Stop();
            Console.WriteLine("Obj.DeepCopy : {0} ms", stopwatch.ElapsedMilliseconds);
        }

        [TestMethod()]
        public void ShallowCopyTest()
        {
            const string str = "231231";
            TestClass t = Obj<TestClass>.New();
            t.Str = str + str;
            st1 s = Obj<st1>.New();
            s.s = str;
            Console.WriteLine(t.Str);
            Console.WriteLine(s.s);
            K ss = Obj<K>.New();
            ss.Field = 1;
            ss.t = t;
            var t1 = t.ShallowCopy();
            var five = (5).ShallowCopy();
            var s1 = Obj<st1>.ShallowCopy(s);
            var ss1 = Obj<K>.ShallowCopy(ss);
            var s2 = s1;
            Console.WriteLine(t1.Str);
            Console.WriteLine(s1.s);
            Console.WriteLine(s2.s);
            Assert.IsNotNull(t);
            Assert.IsNotNull(t1);
            Assert.IsInstanceOfType(t1, typeof(TestClass));
            Assert.IsInstanceOfType(s1, typeof(st1));
            Assert.IsInstanceOfType(ss1, typeof(K));
            Assert.AreEqual(t1.Str, t.Str);
            Assert.AreEqual(s1.s, s.s);
            Assert.IsFalse(Assert.ReferenceEquals(t, t1));
            Assert.IsTrue(Assert.ReferenceEquals(ss.t, ss1.t));
            Assert.AreEqual(five, 5);
        }

        #endregion Public Methods

        #region Private Structs

        private struct K
        {
            #region Public Fields

            public int Field;
            public TestClass t;

            #endregion Public Fields
        }

        #endregion Private Structs

        [TestMethod()]
        public void DeepCopyTest()
        {
            const string str = "231231";
            DeepTestClass t = Obj<DeepTestClass>.New();
            t.Linked = new DeepTestClass();
            t.Linked.Linked = t;
            t.Str = str + str;

            K s;
            s.Field = 1;
            s.t = new TestClass();
            var t1 = Obj.DeepCopy(t);
            var s1 = Obj.DeepCopy(s);
            Assert.IsNotNull(t);
            Assert.IsNotNull(t1);
            Assert.IsInstanceOfType(t1, typeof(DeepTestClass));
            Assert.IsInstanceOfType(s1, typeof(K));
            Assert.AreEqual(t1.Str, t.Str);
            Assert.AreEqual(s1.Field, s.Field);
            Assert.IsFalse(Assert.ReferenceEquals(t, t1));
            Assert.IsFalse(Assert.ReferenceEquals(s.t, s1.t));
        }
    }
}