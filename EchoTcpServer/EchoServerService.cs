using EchoTcpServer.Abstractions;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace EchoTcpServer
{
    // Цей клас містить основну логіку TCP Echo сервера
    public class EchoServerService
    {
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly ITcpListenerFactory _listenerFactory;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private ITcpListenerWrapper? _listener;

        // Конструктор з Dependency Injection
        public EchoServerService(int port, ILogger logger, ITcpListenerFactory listenerFactory)
        {
            // Валідація параметрів
            if(port <= 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            _port = port;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _listenerFactory = listenerFactory ?? throw new ArgumentNullException(nameof(listenerFactory));
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            // Створюємо Listener через фабрику
            _listener = _listenerFactory.Create(IPAddress.Any, _port);
            _listener.Start();
            _logger.Log($"Server started on port {_port}.");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Використовуємо ITcpClientWrapper
                    ITcpClientWrapper client = await _listener.AcceptTcpClientAsync();
                    _logger.Log("Client connected.");

                    // Обробка клієнта в окремому Task
                    _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException)
                {
                    // Listener було закрито під час Stop()
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred in StartAsync: {ex.Message}");
                }
            }

            _logger.Log("Server shutdown.");
        }

        private async Task HandleClientAsync(ITcpClientWrapper client, CancellationToken token)
        {
            // Отримуємо обгорнутий NetworkStream
            using (INetworkStreamWrapper stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        // Echo back
                        await stream.WriteAsync(buffer, 0, bytesRead, token);
                        _logger.Log($"Echoed {bytesRead} bytes to the client.");
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.LogError($"Client handler error: {ex.Message}");
                }
                finally
                {
                    client.Close();
                    _logger.Log("Client disconnected.");
                }
            }
        }

        public void Stop()
        {
            if (_listener == null) return;

            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _cancellationTokenSource.Dispose();
            _logger.Log("Server stopped.");
        }
    }
}