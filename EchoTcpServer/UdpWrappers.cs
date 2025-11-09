using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System;

namespace EchoServer
{
    // Реалізація IUdpClient для робочого коду
    public class UdpClientWrapper : IUdpClient
    {
        private readonly UdpClient _udpClient = new UdpClient();
        public int Send(byte[] datagram, int bytes, IPEndPoint endPoint)
        {
            return _udpClient.Send(datagram, bytes, endPoint);
        }
        public void Dispose() => _udpClient.Dispose();
    }

    // Реалізація IRandomDataGenerator для робочого коду
    public class RandomDataGenerator : IRandomDataGenerator
    {
        private readonly Random _rnd = new Random();
        public byte[] GenerateBytes(int count)
        {
            byte[] samples = new byte[count];
            _rnd.NextBytes(samples);
            return samples;
        }
    }


}