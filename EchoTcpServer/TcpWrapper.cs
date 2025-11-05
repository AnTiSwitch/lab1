using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions;

namespace EchoServer.Implementations
{
    // Обгортка для NetworkStream
    public class NetworkStreamWrapper : INetworkStreamWrapper
    {
        private readonly NetworkStream _stream;

        public NetworkStreamWrapper(NetworkStream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream)); // Додаємо валідацію
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
        {
            return _stream.ReadAsync(buffer, offset, size, cancellationToken);
        }

        public Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
        {
            return _stream.WriteAsync(buffer, offset, size, cancellationToken);
        }

        public void Dispose() => _stream.Dispose();
    }

    // Обгортка для TcpClient
    public class TcpClientWrapper : ITcpClientWrapper
    {
        private readonly TcpClient _client;

        public TcpClientWrapper(TcpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public INetworkStreamWrapper GetStream()
        {
            return new NetworkStreamWrapper(_client.GetStream());
        }

        public void Close() => _client.Close();
        public void Dispose() => _client.Dispose();
    }

    // Обгортка для TcpListener
    public class TcpListenerWrapper : ITcpListenerWrapper
    {
        private readonly TcpListener _listener;

        public TcpListenerWrapper(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
        }

        public void Start() => _listener.Start();
        public void Stop() => _listener.Stop();
        public void Dispose() => ((IDisposable)_listener).Dispose();

        public async Task<ITcpClientWrapper> AcceptTcpClientAsync()
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            return new TcpClientWrapper(client);
        }
    }

    // Реалізація Factory
    public class TcpListenerFactory : ITcpListenerFactory
    {
        public ITcpListenerWrapper Create(IPAddress address, int port)
        {
            return new TcpListenerWrapper(address, port);
        }
    }
}