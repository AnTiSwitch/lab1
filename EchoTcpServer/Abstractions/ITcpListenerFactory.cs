using System.Net;

namespace EchoServerAbstractions
{
    public interface ITcpListenerFactory
    {
        ITcpListenerWrapper Create(IPAddress address, int port); //
    }
}