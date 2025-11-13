using System.Net;
using EchoServer.Abstractions;

namespace EchoServer.Abstractions
{
    // Фабрика для створення TcpListener, дозволяє мокати listener
    public interface ITcpListenerFactory
    {
        ITcpListenerWrapper Create(IPAddress address, int port);
    }
}