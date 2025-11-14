using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions;

namespace EchoServer
{
    public class EchoServerService
    {
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly ITcpListenerFactory _listenerFactory;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private ITcpListenerWrapper _listener = default!;

        // DI: Залежності передаються через конструктор
        public EchoServerService(int port, ILogger logger, ITcpListenerFactory listenerFactory)
        {
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port));
            _port = port;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _listenerFactory = listenerFactory ?? throw new ArgumentNullException(nameof(listenerFactory));
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            try
            {
                // Створення listener через фабрику
                _listener = _listenerFactory.Create(IPAddress.Any, _port);
                _listener.Start();
                _logger.Log($"TCP Echo Server started on port {_port}.");

                var token = _cancellationTokenSource.Token;

                while (!token.IsCancellationRequested)
                {
                    // Асинхронний прийом клієнтів (використовуємо Wrapper)
                    ITcpClientWrapper client = await _listener.AcceptTcpClientAsync();

                    _logger.Log($"Client connected: {((IDisposable)client).ToString()}");

                    // Обробка кожного клієнта в окремому завданні
                    // Використовуємо .ConfigureAwait(false) для оптимізації
                    _ = Task.Run(() => HandleClientAsync(client, token), token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log("Server shut down gracefully.");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                _logger.Log("Listener stopped (Interrupted).");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message}");
            }
            finally
            {
                _listener?.Dispose();
                // Звільнення CancellationTokenSource тепер відбувається у Stop()
            }
        }

        private async Task HandleClientAsync(ITcpClientWrapper client, CancellationToken token)
        {
            try
            {
                using (client) // client - ITcpClientWrapper
                using (var stream = client.GetStream()) // stream - INetworkStreamWrapper
                {
                    var buffer = new byte[1024];
                    int bytesRead;

                    while (!token.IsCancellationRequested)
                    {
                        // Асинхронне читання
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);

                        if (bytesRead == 0) break; // З'єднання закрито

                        // Логіка Echo: відправити назад те, що прочитали
                        await stream.WriteAsync(buffer, 0, bytesRead, token);

                        var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        _logger.Log($"Received and echoed: {received.Trim()}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Це очікувано, коли викликаємо Stop
            }
            catch (Exception ex)
            {
                _logger.LogError($"Client handler error: {ex.Message}");
            }
            finally
            {
                client.Close();
                _logger.Log("Client disconnected.");
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener?.Stop(); // Примусова зупинка AcceptTcpClientAsync

            // ВИПРАВЛЕННЯ S2930: Утилізація CancellationTokenSource
            _cancellationTokenSource.Dispose();

            _logger.Log("Stopping TCP Echo Server...");
        }
    }
}