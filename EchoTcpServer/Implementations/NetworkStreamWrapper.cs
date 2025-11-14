using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions;

namespace EchoServer.Implementations
{
    // Обгортка над системним NetworkStream, реалізує IDisposable
    public class NetworkStreamWrapper : INetworkStreamWrapper
    {
        private readonly NetworkStream _stream;
        private bool _disposed = false; // Додаємо прапорець для відстеження

        public NetworkStreamWrapper(NetworkStream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream)); // Додаємо валідацію
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return _stream.ReadAsync(buffer, offset, count, token);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return _stream.WriteAsync(buffer, offset, count, token);
        }

        // Публічний метод Dispose
        public void Dispose()
        {
            Dispose(true);
            // Запобігаємо повторному виконанню фіналізатора
            GC.SuppressFinalize(this);
        }

        // Захищений метод Dispose для виконання очищення
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Очищення керованих ресурсів (наш NetworkStream)
                    _stream.Dispose();
                }

                // Очищення некерованих ресурсів (відсутні)

                _disposed = true;
            }
        }

        // Фіналізатор (якщо забули викликати Dispose)
        ~NetworkStreamWrapper()
        {
            Dispose(false);
        }
    }
}