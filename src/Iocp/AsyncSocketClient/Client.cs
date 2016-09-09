using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AsyncSocketClient
{
    public class Client
    {
        private TcpClient _client;

        public int Port;

        public IPAddress Remote;

        public Client(int port, IPAddress remote)
        {

            Port = port;
            Remote = remote;
        }

        public void Connect()
        {
            _client = new TcpClient();
            _client.Connect(Remote, Port);
        }
        public void Disconnect()
        {
            _client.Close();
        }
        public void Send(string msg)
        {
            var data = Encoding.Default.GetBytes(msg);
            _client.GetStream().Write(data, 0, data.Length);
        }
    }
}
