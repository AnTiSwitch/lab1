using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using EchoServer.Abstractions;

namespace EchoServer
{
    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly UdpClient _udpClient;
        private Timer _timer=default!;
        private int _messageCounter;

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
            // Створення таймера, який буде викликати SendMessageCallback кожні intervalMilliseconds
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

                // Відправка UDP пакета
                _udpClient.Send(data, data.Length, _host, _port);

                _logger.Log($"UDP sent: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"UDP Sender error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _udpClient.Close();
            _udpClient.Dispose();
            _logger.Log("UDP Sender disposed.");
        }
    }
}