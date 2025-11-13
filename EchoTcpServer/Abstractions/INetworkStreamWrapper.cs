using System.Threading;
using System.Threading.Tasks;
using System;

namespace EchoServerAbstractions
{
    public interface INetworkStreamWrapper : IDisposable
    {
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken); //
        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken); //
    }
}