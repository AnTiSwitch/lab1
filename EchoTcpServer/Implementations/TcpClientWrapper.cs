using System;
using System.Net.Sockets;
using EchoServer.Abstractions;

namespace EchoServer.Implementations
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
            // Повертає обгорнутий NetworkStream
            return new NetworkStreamWrapper(_client.GetStream());
        }

        public void Close()
        {
            _client.Close();
        }

        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}