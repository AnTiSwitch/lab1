// Файл: EchoTcpServer/UdpTimedSender.cs

using EchoTcpServer.Abstractions;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EchoTcpServer
{
    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly UdpClient _udpClient;
        private readonly ILogger _logger;

        // ВИПРАВЛЕНО: Додано '?' (nullable)
        private Timer? _timer;
        private ushort i = 0;
        private readonly Random _random = new Random();

        public UdpTimedSender(string host, int port, ILogger logger)
        {
            _host = host;
            _port = port;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _udpClient = new UdpClient();
            // _timer залишається null до StartSending()
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            // ВИПРАВЛЕНО: Використовуємо лямбда-функцію для безпечної передачі стану
            _timer = new Timer(state => SendMessageCallback(state), null, 0, intervalMilliseconds);
            _logger.Log($"UDP Sender started, sending every {intervalMilliseconds}ms to {_host}:{_port}.");
        }

        // ВИПРАВЛЕНО: Додано '?' до 'object' для відповідності TimerCallback
        private void SendMessageCallback(object? state)
        {
            try
            {
                byte[] samples = new byte[1024];
                _random.NextBytes(samples);
                i++;

                byte[] msg = (new byte[] { 0x04, 0x84 }).Concat(BitConverter.GetBytes(i)).Concat(samples).ToArray();
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

                _udpClient.Send(msg, msg.Length, endpoint);
                _logger.Log($"Message sent (ID: {i}) to {_host}:{_port} ");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message: {ex.Message}");
            }
        }

        public void StopSending()
        {
            // Використовуємо безпечну навігацію
            _timer?.Dispose();
            _timer = null;
            _logger.Log("UDP Sender stopped.");
        }

        public void Dispose()
        {
            StopSending();
            _udpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}