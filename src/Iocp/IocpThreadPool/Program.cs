using System;
using System.Threading;

namespace IocpThreadPool
{
    class Program
    {
        static void Main()
        {
            // Create the MSSQL IOCP Thread Pool
            var pThreadPool = new SafeIocpThreadPool(0, 5, 10, IocpThreadFunction);
            for (var i = 0; i < 100; i++)
                pThreadPool.PostEvent(new MyData {Value = i});

            pThreadPool.Dispose();
            Console.WriteLine("Disposed");
            Console.ReadLine();
        }


        private static void IocpThreadFunction(MyData obj)
        {
            try
            {
                Console.WriteLine("Value: {0},Thread:{1}", obj.Value, Thread.CurrentThread.Name);
            }
            catch (Exception pException)
            {
                Console.WriteLine(pException.Message);
            }
        }
    }
}
