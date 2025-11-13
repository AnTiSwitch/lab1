using System;
using System.Net.Sockets;
using EchoServer.Abstractions;

namespace EchoServer.Implementations
{
    // Обгортка над системним TcpClient
    public class TcpClientWrapper : ITcpClientWrapper
    {
        private readonly TcpClient _client;
        private bool _disposed = false; // Додаємо прапорець для відстеження

        public TcpClientWrapper(TcpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public INetworkStreamWrapper GetStream()
        {
            // Повертає обгорнутий NetworkStream
            return new NetworkStreamWrapper(_client.GetStream());
        }

        public void Close()
        {
            _client.Close();
        }

        // 1. Публічний метод Dispose (викликає приватний/захищений)
        public void Dispose()
        {
            Dispose(true);
            // Запобігаємо повторному виконанню фіналізатора
            GC.SuppressFinalize(this);
        }

        // 2. Захищений метод Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Очищення керованих ресурсів: TcpClient
                    _client?.Dispose();
                }

                _disposed = true;
            }
        }

        // 3. Фіналізатор
        ~TcpClientWrapper()
        {
            Dispose(false);
        }
    }
}