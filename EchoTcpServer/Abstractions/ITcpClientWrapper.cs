using System;

namespace EchoServerAbstractions
{
    public interface ITcpClientWrapper : IDisposable
    {
        INetworkStreamWrapper GetStream(); //
        void Close(); //
    }
}