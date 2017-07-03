using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EP.Ex.Tests
{
    [TestClass()]
    public class ObjTests
    {
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
            TestClass c = Obj.New<TestClass>();
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
            const int count = 100000;
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
                var obj = (object)Obj.DeepCopy((object)co);
            }
            stopwatch.Stop();
            Console.WriteLine("Obj.DeepCopy : {0} ms", stopwatch.ElapsedMilliseconds);
        }

        [TestMethod()]
        public void NewWONoArgConstructorTest()
        {
            var obj = new { test = 1, other = "string" };
            var dup = obj.DeepCopy();
            Assert.AreEqual(obj.test, dup.test);
            Assert.AreEqual(obj.other, dup.other);
            Assert.IsFalse(Object.ReferenceEquals(obj, dup));
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

        #region Private Fields

        private const string str = "231231";

        private object[,,,,] MulDimArr = new object[1, 1, 2, 2, 2] {
            {
                {
                    {
                        { "test", 1 },
                        { 2, 2 }
                    },
                    {
                        { 3, 4 },
                        { "rer", 5 }
                    }
                }
            }
        };

        private int[,,,,] MulDimArrValued = new int[1, 1, 2, 2, 2] {
            {
                {
                    {
                        { 5, 1 },
                        { 2, 2 }
                    },
                    {
                        { 3, 4 },
                        { 7, 5 }
                    }
                }
            }
        };

        #endregion Private Fields

        public static Dictionary<object, object> CloneDict(Dictionary<object, object> src, Dictionary<object, object> dict)
        {
            var dst = new Dictionary<object, object>();
            object key;
            object value;
            foreach (var p in src)
            {
                if (!dict.TryGetValue(p.Key, out key))
                {
                    key = dict[p.Key] = p.Key.DeepCopy();
                }
                if (!dict.TryGetValue(p.Value, out value))
                {
                    value = dict[p.Value] = p.Value.DeepCopy();
                }
                dst[key] = value;
            }
            return dst;
        }

        [TestMethod()]
        public void DeepCopyHSTest()
        {
            var ht1 = new HashSet<object>(new object[] { "test", 1, 2, 3, 4, 5 });
            var ht2 = ht1.DeepCopy();
            Assert.IsFalse(Assert.ReferenceEquals(ht1, ht2));
        }
        [TestMethod()]
        public void DateTimeTest()
        {
            var dt = DateTime.Now;
            var dt1 = dt.DeepCopy();
            Assert.AreEqual(dt, dt1);
        }
        [TestMethod]
        public void GenericTest()
        {
            var sn = new TestNode();
            sn.fld = Obj.DeepCopy(DateTime.Now);
            var sn1 = sn.DeepCopy();
            Assert.AreEqual(sn.fld, sn1.fld);
            Assert.IsFalse(Object.ReferenceEquals(sn, sn1));
        }
        [TestMethod()]
        public void DeepCopyMultiArrayTest()
        {
            var ma = MulDimArr;
            ma[0, 0, 1, 1, 1] = ma;
            var ma2 = ma.DeepCopy();
            var mav = MulDimArrValued;
            var mav2 = mav.DeepCopy();
            Assert.IsFalse(Assert.ReferenceEquals(ma, ma2));
            Assert.IsFalse(Assert.ReferenceEquals(mav, mav2));
            Assert.IsFalse(Assert.ReferenceEquals(ma[0, 0, 1, 1, 1], ma2[0, 0, 1, 1, 1]));
            Assert.IsTrue(Assert.ReferenceEquals(ma[0, 0, 1, 1, 1], ma));
            Assert.IsTrue(Assert.ReferenceEquals(ma2[0, 0, 1, 1, 1], ma2));
        }

        [TestMethod()]
        public void DeepCopyTest()
        {
            object[] arr = new object[2] { 1, "str" };
            var obj1 = arr.ShallowCopy();
            var p = Obj<object[]>.ShallowCopy(arr);
            DeepTestClass t = Obj<DeepTestClass>.New();
            t.Linked = new DeepTestClass();
            t.Linked.Linked = t;
            t.Str = str + str;
            var arr2 = arr.DeepCopy();
            K s;
            s.Field = 1;
            s.t = new TestClass();
            var t1 = Obj.DeepCopy(t);
            var s1 = Obj.DeepCopy(s);
            Dictionary<object, object> d = new Dictionary<object, object>() { { t, t } };

            //Obj<Dictionary<object, object>>.SetDeepCopyFn(CloneDict);
            var d2 = d.DeepCopy();
            var k = d2.Keys.First();
            var k1 = d2[k];
            HashSet<DeepTestClass> hs = new HashSet<DeepTestClass>();
            hs.Add(t);
            hs.Add(t1);
            Hashtable ht = new Hashtable();
            ht[t] = t;
            ht[t1] = t1;
            var htc = ht.DeepCopy();
            var hsc = hs.DeepCopy();
            Assert.IsTrue(k == k1);
            Assert.IsNotNull(t);
            Assert.IsNotNull(t1);
            Assert.IsInstanceOfType(t1, typeof(DeepTestClass));
            Assert.IsInstanceOfType(s1, typeof(K));
            Assert.AreEqual(t1.Str, t.Str);
            Assert.AreEqual(s1.Field, s.Field);
            Assert.AreEqual(hs.Count, hsc.Count);
            Assert.IsFalse(Object.ReferenceEquals(hs.ElementAt(0), hsc.ElementAt(0)));
            Assert.AreEqual(ht.Count, htc.Count);
            Assert.IsFalse(Object.ReferenceEquals(ht, htc));
            foreach (var h in htc.Keys)
            {
                Assert.IsTrue(Object.ReferenceEquals(htc[h], h));
            }
            Assert.IsFalse(Assert.ReferenceEquals(t, t1));
            Assert.IsFalse(Assert.ReferenceEquals(s.t, s1.t));
        }

        [TestMethod()]
        public void InheritenseLinkedPropertyCopyTest()
        {
            var a = Obj<A>.New();
            var b = Obj<B>.New();

            a.other = b;
            b.other = a;
            var c = a.DeepCopy();

            Assert.IsTrue(Object.ReferenceEquals(c, c.other.other));
        }

        [TestMethod()]
        public void LinkedDeepCopyTest()
        {
            DeepTestClass t = Obj<DeepTestClass>.New();
            t.Linked = new DeepTestClass();
            t.Linked.Linked = t;
            t.Str = str + str;
            var t1 = Obj.DeepCopy(t);
            Assert.IsTrue(Object.ReferenceEquals(t1, t1.Linked.Linked));
        }

        [TestMethod()]
        public void ShallowMultiArrayTest()
        {
            var ma2 = MulDimArr.ShallowCopy();
            Assert.IsFalse(Assert.ReferenceEquals(MulDimArr, ma2));
        }
        [TestMethod()]
        public void ArrayStructCopyTest()
        {
            DateTime[] s = new DateTime[] { DateTime.Now, DateTime.UtcNow };
            var s1 = Obj.DeepCopy(s);
            Assert.IsInstanceOfType(s1, typeof(DateTime[]));
            Assert.AreEqual(s1[0], s[0]);
        }
        [TestMethod()]
        public void StructCopyTest()
        {
            K s;
            s.Field = 1;
            s.t = new TestClass();
            var s1 = Obj.DeepCopy(s);
            Assert.IsInstanceOfType(s1, typeof(K));
            Assert.AreEqual(s1.Field, s.Field);
            Assert.IsFalse(Assert.ReferenceEquals(s.t, s1.t));
        }
    }
}