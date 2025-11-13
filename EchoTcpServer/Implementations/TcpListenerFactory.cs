using System.Net;
using EchoServer.Abstractions;

namespace EchoServer.Implementations
{
    // Фабрика, яка створює обгорнутий listener
    public class TcpListenerFactory : ITcpListenerFactory
    {
        public ITcpListenerWrapper Create(IPAddress address, int port)
        {
            return new TcpListenerWrapper(address, port);
        }
    }
}