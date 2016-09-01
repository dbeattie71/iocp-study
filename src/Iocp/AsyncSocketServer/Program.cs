using System;
using AsyncSocketServer.Core;

namespace AsyncSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server(9099, 1024);
            server.Start();
            Console.WriteLine("服务器已启动....");
            Console.ReadLine();
        }
    }
}
