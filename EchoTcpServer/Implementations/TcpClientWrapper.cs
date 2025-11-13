using System.Net.Sockets;
using EchoServerAbstractions;

namespace EchoServerImplementations
{
    public class TcpClientWrapper : ITcpClientWrapper
    {
        private readonly TcpClient _client;

        public TcpClientWrapper(TcpClient client) //
        {
            _client = client;
        }

        public INetworkStreamWrapper GetStream() //
        {
            [cite_start]// Отримує NetworkStream і обгортає його в NetworkStreamWrapper [cite: 758]
            return new NetworkStreamWrapper(_client.GetStream());
        }

        public void Close() //
        {
            _client.Close();
        }

        public void Dispose() //
        {
            _client.Dispose();
        }
    }
}