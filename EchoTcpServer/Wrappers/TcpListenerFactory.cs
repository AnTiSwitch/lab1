using EchoTcpServer.Abstractions;
using System.Net;
using EchoTcpServer.Wrappers; // Потрібен для створення конкретного Wrapper

namespace EchoTcpServer.Wrappers
{
    // Фабрика для створення TcpListenerWrapper
    public class TcpListenerFactory : ITcpListenerFactory
    {
        public ITcpListenerWrapper Create(IPAddress address, int port)
        {
            return new TcpListenerWrapper(address, port);
        }
    }
}