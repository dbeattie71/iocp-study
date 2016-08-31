using System;
using System.Linq.Expressions;

namespace ExpressTreeDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var mymath = new MyMath();
            var add = typeof(MyMath).GetMethod("Add");
            var math = Expression.Parameter(typeof(MyMath));
            var a = Expression.Parameter(typeof(int), "a");
            var b = Expression.Parameter(typeof(int), "b");
            var body = Expression.Call(math, add, a, b);
            var lambda = Expression.Lambda<Func<MyMath, int, int, int>>(body, math, a, b);

            var addFunc = lambda.Compile();
            Console.WriteLine(addFunc(mymath, 1, 2));
            Console.Read();
        }
    }

    public class MyMath
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
    }
}
