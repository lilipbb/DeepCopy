using System;
using System.Collections.Generic;
using Xunit;

namespace Test
{
    public class CopyTest
    {
        class A
        {
            public int a;
            public string b;
            public long[] c;
        }
        struct B
        {
            public int a;
            public long[] c;
            public string d;
        }
        [Fact]
        public void TestCase() {
            var a = new A() { a = 1, b = "2", c = new[] { 111l, 222 } };
            a.DeepCopyCase(out B b);
            Assert.Equal(b.a, a.a);
            Assert.Equal(a.c, b.c);
            a.c[0] = 1;
            Assert.NotEqual(a.c[0], b.c[0]);
            var aa = new object[] { 1, 2,"wode" };
            var bb = aa.DeepCopy();
            Assert.NotSame(aa, bb);
            Assert.Equal((int)bb[0], 1);
        }
        [Fact]
        public void TestBase() {
            var aint = 1;
            var bint=aint.DeepCopy();
            Assert.Equal(aint, bint);
            var astring = "helloworld";
            string bstring= astring.DeepCopy();
            Assert.Equal(astring, bstring);
            var atuple = (1, "wode", 123);
            atuple.DeepCopyCase( out (int, string, int) btuple);
            Assert.Equal(atuple, btuple);
        }
        [Fact]
        public void TestArray() {
            var a = new[] { 1, 2, 3, 4 };
            var b=a.DeepCopy();
            Assert.Equal(a, b);
            var a1 = new int[][]{ new[]{1,2,3},new[] { 4,5,6} };
            var b1=a1.DeepCopy();
            Assert.Equal(a1, b1);
            Assert.NotSame(a1, b1);
        }
        [Fact]
        public void TestMultiplyArray() {
            var a = new[, ,] { { { 1, 2 }, { 3, 4 } }, { { 5, 6 }, { 7, 8 } } };
            var b=a.DeepCopy();
            Assert.Equal(a, b);
            Assert.NotSame(a, b);
        }

        struct S1
        {
            public string s;
            public int[] a;
            public S2 struct2;
        }
        struct S2
        {
            public long num;
        }
        [Fact]
        public void TestStruct() {
            S1 s;
            s.a = new int[4] { 1, 2, 3, 4 };
            s.s = "hello";
            s.struct2 = new S2() { num=100};
            s.DeepCopyCase(out S1 s1);
            Assert.Equal(s.a, s1.a);
            Assert.Equal(s.s, s1.s);
            Assert.Equal(s.struct2.num, 100L);
            Assert.NotSame(s, s1);
        }
        class C1
        {
            public int a;
            public string b;
            public S1 c;
            public int d { get; set; }
            private string f;
            public string ff { get => f; set => f = value; }
        }
      
        [Fact]
        public void TestClass() {
            var class1 = new C1() { a = 10, b = "wo", d = 11, ff = "hello" };
            class1.DeepCopyCase(out C1 outClass);
            Assert.Equal(class1.a, outClass.a);
            Assert.Equal(class1.b, outClass.b);
            Assert.Equal(class1.c, outClass.c);
            Assert.Equal(class1.d, outClass.d);
            Assert.Equal(class1.ff, outClass.ff);
            Assert.NotSame(class1, outClass);
        }

        [Fact]
        public void TestList() {
            IList<int> a = new List<int>() { 1, 2, 3 };
            var b= a.DeepCopy();
            Assert.Equal(a, b);
            Assert.NotSame(a, b);
        }

        [Fact]
        public void DictoryTest() {
            var dic = new Dictionary<int,C1>();
            dic[123] = new C1() {c=new S1() { a=new[] { 1,2,3} } };
            var dic1 = dic.DeepCopy();
            Assert.Equal(dic1[123].c.a[1], 2);
            Assert.NotSame(dic,dic1);
        }
        [Fact]
        public void BoundArrayTest() {
            var arr = new [,,] { { { 1,2,3 }, {4,5,6 } }, { { 7,8,9 }, { 10,11,12 } } };
            var result = arr.DeepCopy();
            Assert.Equal(arr, result);
            Assert.NotSame(arr, result);
        }

        [Fact]
        public void MultiplyArrayTest() {
            var arr = new int[][]
            {
                new [] {1,2,3},
                new [] {4,5,6,7,},
                new [] {8}
            };
            var result = arr.DeepCopy();
            Assert.Equal(arr, result);
            Assert.NotSame(arr, result);
        }

        [Fact]
        public void BoundMultiplyArrayTest() {
            var arr = new int[2][,]
            {
                new [,] { {1,3}, {5,7} },
                new [,] { {0,2}, {4,6}, {8,10} },
            };
            var result = arr.DeepCopy();
            Assert.Equal(arr, result);
            Assert.NotSame(arr, result);
        }
    }
}
