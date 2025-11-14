using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using EchoServer.Abstractions;

namespace EchoServer
{
    // Цей клас потребує IDisposable (S3881)
    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly UdpClient _udpClient;

        private Timer _timer = default!; // Виправлення CS8618
        private int _messageCounter;

        // ДОДАНО: Прапорець для відстеження стану утилізації
        private bool _disposed = false;

        // ... (Конструктор, StartSending, SendMessageCallback залишаються без змін)

        // DI: Залежність ILogger передається через конструктор
        public UdpTimedSender(string host, int port, ILogger logger)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
            _port = port;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _udpClient = new UdpClient();
        }

        public void StartSending(int intervalMilliseconds)
        {
            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
            _logger.Log($"UDP Sender started, sending every {intervalMilliseconds}ms to {_host}:{_port}");
        }

        private void SendMessageCallback(object state)
        {
            try
            {
                _messageCounter++;
                string message = $"UDP broadcast: Message {_messageCounter}";
                byte[] data = Encoding.UTF8.GetBytes(message);

                _udpClient.Send(data, data.Length, _host, _port);

                _logger.Log($"UDP sent: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"UDP Sender error: {ex.Message}");
            }
        }

        // ПОВНИЙ ШАБЛОН IDISPOSABLE:

        // 1. Публічний метод Dispose
        public void Dispose()
        {
            Dispose(true);
            // Викликати GC.SuppressFinalize, якщо очищення було успішним
            GC.SuppressFinalize(this);
        }

        // 2. Захищений метод Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Очищення керованих ресурсів: Timer та UdpClient
                    _timer?.Dispose();
                    _udpClient?.Close();
                    _udpClient?.Dispose();
                    _logger.Log("UDP Sender disposed."); // Логування зупинки
                }

                _disposed = true;
            }
        }

        // 3. Фіналізатор (забезпечує очищення, якщо Dispose не викликано)
        ~UdpTimedSender()
        {
            Dispose(false);
        }
    }
}