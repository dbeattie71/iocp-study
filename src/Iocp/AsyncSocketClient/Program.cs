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

            c.connect();
            Console.WriteLine("服务器连接成功!");
            while (true)
            {
                Console.Write("send>");
                string msg = Console.ReadLine();
                if (msg == "exit")
                    break;
                c.send(msg);
            }
            c.disconnect();
            Console.ReadLine();
        }
    }
}
