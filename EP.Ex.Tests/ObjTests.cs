﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using EP.Ex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

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
        public struct st1
        {
            public string s;
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
        public void NewPerformanceTest()
        {
            const int count = 100000000;
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
    }
}