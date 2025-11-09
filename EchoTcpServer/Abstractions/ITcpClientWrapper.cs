using System;

namespace EchoTcpServer.Abstractions
{
    public interface ITcpClientWrapper : IDisposable
    {
        INetworkStreamWrapper GetStream();
        void Close();
    }
}