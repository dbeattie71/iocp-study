using System;

namespace AsyncSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new ServerCore(8088, 1024);
            server.Start();
            Console.WriteLine("服务器已启动....");
            Console.ReadLine();
        }
    }
}
