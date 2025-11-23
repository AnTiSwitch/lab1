using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer.Abstractions
{
    public class NetworkStreamWrapper : INetworkStreamWrapper
    {
        private readonly Stream _stream;

        public NetworkStreamWrapper(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return _stream.ReadAsync(buffer, offset, count, token);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return _stream.WriteAsync(buffer, offset, count, token);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}