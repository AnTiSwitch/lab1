using System.Net;
using EchoServerAbstractions;

namespace EchoServerImplementations
{
    public class TcpListenerFactory : ITcpListenerFactory
    {
        public ITcpListenerWrapper Create(IPAddress address, int port) //
        {
            return new TcpListenerWrapper(address, port);
        }
    }
}