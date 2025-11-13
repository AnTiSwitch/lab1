using System;
using EchoServer.Abstractions;

namespace EchoServer.Abstractions
{
    // Обгортка для TcpClient
    public interface ITcpClientWrapper : IDisposable
    {
        INetworkStreamWrapper GetStream();
        void Close();
    }
}