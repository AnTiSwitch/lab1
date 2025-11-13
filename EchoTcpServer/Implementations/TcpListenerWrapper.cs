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

        public TcpListenerWrapper(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
        }

        public void Start()
        {
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

        public void Dispose()
        {
            // Stop() виконує dispose
            Stop();
        }
    }
}