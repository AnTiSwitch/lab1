using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using EchoServerAbstractions;

namespace EchoServerServices
{
    public class UdpTimedSender : IDisposable
    {
        private readonly string _host; //
        private readonly int _port; //
        private readonly ILogger _logger; //
        private readonly UdpClient _udpClient; //
        private readonly Random _random; //
        private Timer _timer; //
        private ushort _counter = 0; //

        public UdpTimedSender(string host, int port, ILogger logger)
        {
            if (string.IsNullOrEmpty(host)) throw new ArgumentException("Host cannot be null or empty.", nameof(host));
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _host = host;
            _port = port;
            _logger = logger;
            _udpClient = new UdpClient(); // Створює UDP клієнт [cite: 617]
            _random = new Random(); //
        }

        public void StartSending(int interval) //
        {
            if (_timer != null) return;

            [cite_start]// Створює Timer, який викликає SendMessageCallback [cite: 623]
            _timer = new Timer(SendMessageCallback, null, 0, interval);
        }

        private void SendMessageCallback(object state) //
        {
            try
            {
                var data = new byte[1024]; // Створює масив 1024 байти [cite: 629]
                _random.NextBytes(data); // Заповнює випадковими даними [cite: 630]

                _counter++; // Збільшує лічильник [cite: 631]

                [cite_start]// Формування пакету: [0x04, 0x84] + counter + дані [cite: 632, 633, 634, 635]
                var packet = new byte[1028];
                packet[0] = 0x04;
                packet[1] = 0x84;

                var counterBytes = BitConverter.GetBytes(_counter);
                Array.Copy(counterBytes, 0, packet, 2, 2);
                Array.Copy(data, 0, packet, 4, data.Length);

                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

                _udpClient.Send(packet, packet.Length, endpoint); // Відправляє UDP пакет
                _logger.Log($"Message sent to {_host}:{_port}"); //
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending UDP message: {ex.Message}");
            }
        }

        public void StopSending() //
        {
            _timer?.Dispose(); // Зупиняє і звільняє таймер [cite: 648]
            _timer = null;
        }

        public void Dispose() //
        {
            StopSending(); //
            _udpClient.Close(); // Закриває UdpClient [cite: 653]
        }
    }
}