using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using EchoServer.Abstractions;

namespace EchoServer.Services
{
    // Клас залишається, але приймає ILogger
    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly ILogger _logger; // Нова залежність
        private readonly UdpClient _udpClient; // UdpClient залишаємо тут, але можна теж абстрагувати
        private readonly Random _random;
        private Timer? _timer;
        private ushort _counter = 0; // Змінено назву на _counter

        public UdpTimedSender(string host, int port, ILogger logger)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port > 0 ? port : throw new ArgumentOutOfRangeException(nameof(port));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _udpClient = new UdpClient();
            _random = new Random();
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            // Валідація інтервалу
            if (intervalMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds));

            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
            _logger.Log($"UDP Sender started, sending every {intervalMilliseconds}ms.");
        }

        private void SendMessageCallback(object state)
        {
            try
            {
                // dummy data
                byte[] samples = new byte[1024];
                _random.NextBytes(samples);
                _counter++;

               // Формування пакету: [0x04, 0x84] + counter + samples [cite: 414]
                byte[] msg = (new byte[] { 0x04, 0x84 }).Concat(BitConverter.GetBytes(_counter)).Concat(samples).ToArray();
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

                _udpClient.Send(msg, msg.Length, endpoint);
                _logger.Log($"Message # {_counter} sent to {_host}:{_port}"); // Використовуємо ILogger
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message: {ex.Message}"); // Використовуємо ILogger
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
            _logger.Log("UDP Sender stopped.");
        }

        public void Dispose()
        {
            StopSending();
            _udpClient.Dispose();
        }
    }
}