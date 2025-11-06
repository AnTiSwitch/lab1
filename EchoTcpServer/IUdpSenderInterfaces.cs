using System;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace EchoServer
{
    // Інтерфейс для потоку (для ClientEchoHandler)
    public interface INetworkStream : IDisposable
    {
        Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken);
        Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken);
    }

    // Інтерфейси для UDP-частини (якщо ви вирішите її рефакторити):
    public interface IUdpClient : IDisposable
    {
        int Send(byte[] datagram, int bytes, IPEndPoint endPoint);
    }
    public interface IRandomDataGenerator
    {
        byte[] GenerateBytes(int count);
    }
}