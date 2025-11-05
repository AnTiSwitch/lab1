using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer.Abstractions
{
    // Абстракція для NetworkStream
    public interface INetworkStreamWrapper : IDisposable
    {
        Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken);
        Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken);
    }

    // Абстракція для TcpClient
    public interface ITcpClientWrapper : IDisposable
    {
        INetworkStreamWrapper GetStream();
        void Close();
    }

    // Абстракція для TcpListener
    public interface ITcpListenerWrapper : IDisposable
    {
        void Start();
        void Stop();
        Task<ITcpClientWrapper> AcceptTcpClientAsync();
    }

    // Фабрика для створення Listener
    public interface ITcpListenerFactory
    {
        ITcpListenerWrapper Create(IPAddress address, int port);
    }
}