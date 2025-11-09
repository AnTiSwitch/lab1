using EchoTcpServer.Abstractions;
using System.Net.Sockets;

namespace EchoTcpServer.Wrappers
{
    // Обгортка над системним TcpClient
    public class TcpClientWrapper : ITcpClientWrapper
    {
        private readonly TcpClient _client;

        public TcpClientWrapper(TcpClient client)
        {
            _client = client;
        }

        public INetworkStreamWrapper GetStream()
        {
            // Повертаємо обгорнутий NetworkStream
            return new NetworkStreamWrapper(_client.GetStream());
        }

        public void Close() => _client.Close();

        public void Dispose() => _client.Dispose();
    }
}