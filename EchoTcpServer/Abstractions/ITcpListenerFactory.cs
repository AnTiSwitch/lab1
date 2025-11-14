using System.Net;
using EchoServer.Abstractions;

namespace EchoServer.Abstractions
{
    // ������� ��� ��������� TcpListener, �������� ������ listener
    public interface ITcpListenerFactory
    {
        ITcpListenerWrapper Create(IPAddress address, int port);
    }
}