using System;
using EchoServer.Abstractions;

namespace EchoServer.Abstractions
{
    // �������� ��� TcpClient
    public interface ITcpClientWrapper : IDisposable
    {
        INetworkStreamWrapper GetStream();
        void Close();
    }
}