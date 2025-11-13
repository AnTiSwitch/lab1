using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions;

namespace EchoServer.Implementations
{
    // Обгортка над системним NetworkStream
    public class NetworkStreamWrapper : INetworkStreamWrapper
    {
        private readonly NetworkStream _stream;

        public NetworkStreamWrapper(NetworkStream stream)
        {
            _stream = stream;
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            // Делегує виклик до внутрішнього (реального) потоку
            return _stream.ReadAsync(buffer, offset, count, token);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            // Делегує виклик до внутрішнього (реального) потоку
            return _stream.WriteAsync(buffer, offset, count, token);
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}