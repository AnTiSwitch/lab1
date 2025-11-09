using System.Net;

namespace EchoTcpServer.Abstractions
{
    public interface ITcpListenerFactory
    {
        ITcpListenerWrapper Create(IPAddress address, int port);
    }
}