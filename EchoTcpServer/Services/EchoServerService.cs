using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions;
using EchoServer.Implementations; // Потрібно для TcpListenerWrapper, якщо він не у папці Implementations

namespace EchoServer.Services
{
    // Це рефакторений клас EchoServer
    public class EchoServerService : IDisposable
    {
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly ITcpListenerFactory? _listenerFactory;
        private ITcpListenerWrapper _listener; // Тепер використовуємо Wrapper
        private CancellationTokenSource _cancellationTokenSource;

        // DI: Впроваджуємо всі залежності (Logger, Factory)
        public EchoServerService(int port, ILogger logger, ITcpListenerFactory listenerFactory)
        {
            // Валідація вхідних параметрів
            _port = port > 0 ? port : throw new ArgumentOutOfRangeException(nameof(port));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _listenerFactory = listenerFactory ?? throw new ArgumentNullException(nameof(listenerFactory));

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            // Створюємо listener через Factory
            _listener = _listenerFactory.Create(IPAddress.Any, _port);
            _listener.Start();
            _logger.Log($"Server started on port {_port}.");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // ВИКОРИСТОВУЄМО АБСТРАКЦІЮ ITcpClientWrapper
                    ITcpClientWrapper client = await _listener.AcceptTcpClientAsync();
                    _logger.Log("Client connected.");

                    // Запускаємо обробку клієнта
                    _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException)
                {
                    // Listener було закрито під час виклику Stop()
                    break;
                }
                catch (Exception ex)
                {
                    // Обробка некритичних помилок у циклі
                    if (!_cancellationTokenSource.Token.IsCancellationRequested)
                        _logger.LogError($"Error accepting client: {ex.Message}");
                }
            }

            _logger.Log("Server shutdown.");
        }

        // Приймає ITcpClientWrapper та використовує INetworkStreamWrapper
        private async Task HandleClientAsync(ITcpClientWrapper client, CancellationToken token)
        {
            // ВИКОРИСТОВУЄМО АБСТРАКЦІЮ INetworkStreamWrapper
            using (INetworkStreamWrapper stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead, token);
                        _logger.Log($"Echoed {bytesRead} bytes to the client.");
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.LogError($"Error handling client: {ex.Message}");
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
            _cancellationTokenSource.Cancel();
            _listener.Stop();
        }

        public void Dispose()
        {
            Stop();
            _listener?.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}