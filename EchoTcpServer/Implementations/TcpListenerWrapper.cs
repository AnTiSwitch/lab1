using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using EchoServer.Abstractions;

namespace EchoServer.Implementations
{
    // Обгортка над системним TcpListener
    public class TcpListenerWrapper : ITcpListenerWrapper
    {
        private readonly TcpListener _listener;
        private bool _disposed = false; // Додаємо прапорець для відстеження

        public TcpListenerWrapper(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
        }

        public void Start()
        {
            // Перевірка стану не потрібна, якщо використовується Dispose
            _listener.Start();
        }

        public void Stop()
        {
            _listener.Stop();
        }

        public async Task<ITcpClientWrapper> AcceptTcpClientAsync()
        {
            // Приймає реальний TcpClient
            var client = await _listener.AcceptTcpClientAsync();

            // Обгортає його у наш інтерфейс
            return new TcpClientWrapper(client);
        }

        // 1. Публічний метод Dispose (викликає захищений)
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
                    // Очищення керованих ресурсів: TcpListener
                    // Викликаємо Stop(), який утилізує ресурси
                    Stop();
                }

                // Очищення некерованих ресурсів (відсутні)

                _disposed = true;
            }
        }

        // 3. Фіналізатор
        ~TcpListenerWrapper()
        {
            Dispose(false);
        }
    }
}