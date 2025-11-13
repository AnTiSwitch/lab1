using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoServerAbstractions;

namespace EchoServerImplementations
{
    public class NetworkStreamWrapper : INetworkStreamWrapper
    {
        private readonly NetworkStream _stream; //

        public NetworkStreamWrapper(NetworkStream stream) //
        {
            _stream = stream;
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) //
        {
            return _stream.ReadAsync(buffer, offset, count, cancellationToken); // Делегує читання [cite: 776]
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) //
        {
            return _stream.WriteAsync(buffer, offset, count, cancellationToken); // Делегує запис [cite: 789]
        }

        public void Dispose() //
        {
            _stream.Dispose(); // Закриває потік [cite: 796]
        }
    }
}