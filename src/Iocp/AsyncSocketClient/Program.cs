using System;
using System.Net;

namespace AsyncSocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress remote = IPAddress.Parse("127.0.0.1");
            var c = new Client(9099, remote);

            c.Connect();
            Console.WriteLine("服务器连接成功!");
            while (true)
            {
                Console.Write("send>");
                string msg = Console.ReadLine();
                if (msg == "exit")
                    break;
                c.Send(msg);
            }
            c.Disconnect();
            Console.ReadLine();
        }
    }
}
