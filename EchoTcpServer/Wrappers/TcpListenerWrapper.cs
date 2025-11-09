using EchoTcpServer.Abstractions;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace EchoTcpServer.Wrappers
{
    // Обгортка над системним TcpListener
    public class TcpListenerWrapper : ITcpListenerWrapper
    {
        private readonly TcpListener _listener;

        public TcpListenerWrapper(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
        }

        public void Start() => _listener.Start();
        public void Stop() => _listener.Stop();

        public async Task<ITcpClientWrapper> AcceptTcpClientAsync()
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            // Обгортаємо системний TcpClient
            return new TcpClientWrapper(client);
        }
    }
}