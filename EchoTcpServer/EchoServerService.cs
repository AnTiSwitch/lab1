using EchoServerAbstractions;
using NetSdrClientApp.Networking;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServerServices
{
    public class EchoServerService
    {
        private readonly int _port; //
        private readonly ILogger _logger; //
        private readonly ITcpListenerFactory _listenerFactory; //
        private readonly CancellationTokenSource _cancellationTokenSource; //
        private ITcpListenerWrapper _listener; //

        [cite_start]// Dependency Injection через конструктор [cite: 195]
        public EchoServerService(int port, ILogger logger, ITcpListenerFactory listenerFactory)
        {
            [cite_start]// Валідація параметрів (згідно звіту) [cite: 77]
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (listenerFactory == null) throw new ArgumentNullException(nameof(listenerFactory));

            _port = port;
            _logger = logger;
            _listenerFactory = listenerFactory;
            _cancellationTokenSource = new CancellationTokenSource(); //
        }

        public async Task StartAsync() //
        {
            [cite_start]// Створює listener через фабрику для вказаного порту [cite: 558]
            _listener = _listenerFactory.Create(IPAddress.Any, _port);
            _listener.Start(); // Починає слухати [cite: 559]
            _logger.Log($"Server started on port {_port}."); //

            var token = _cancellationTokenSource.Token;
            try
            {
                [cite_start]// Нескінченний цикл [cite: 561]
                while (!token.IsCancellationRequested)
                {
                    var clientWrapper = await _listener.AcceptTcpClientAsync(); // Чекає підключення [cite: 562]

                    [cite_start]// Запускає HandleClientAsync() в окремому Task [cite: 564]
                    Task.Run(() => HandleClientAsync(clientWrapper, token), token);
                }
            }
            catch (OperationCanceledException)
            {
                // Вихід з циклу при зупинці
            }
            finally
            {
                _logger.Log("Server shutdown"); //
            }
        }

        [cite_start]
        private async Task HandleClientAsync(ITcpClientWrapper clientWrapper, CancellationToken token) // Обробка одного клієнта [cite: 569]
        {
            // Використовуємо using для коректного звільнення ресурсів клієнта
            using (clientWrapper)
            {
                _logger.Log("Client connected."); //

                try
                {
                    [cite_start]// Отримує NetworkStream від клієнта [cite: 571]
                    using (var stream = clientWrapper.GetStream())
                    {
                        var buffer = new byte[8192]; // Створює буфер [cite: 572]
                        int bytesRead;

                        [cite_start]// Цикл обробки: ReadAsync() -> WriteAsync() [cite: 573]
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                        {
                            await stream.WriteAsync(buffer, 0, bytesRead, token); // Відправляє ТІ Ж дані назад [cite: 578]
                            _logger.Log($"Echoed {bytesRead} bytes to the client."); //
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error handling client: {ex.Message}"); // При помилці логує помилку [cite: 580]
                }

                // Закриває з'єднання
                _logger.Log("Client disconnected"); //
            }
        }

        [cite_start]
        public void Stop() // Зупинка сервера [cite: 592]
        {
            _cancellationTokenSource.Cancel(); // Викликає Cancel() на токені [cite: 594]
            _listener?.Stop(); // Зупиняє listener [cite: 596]
            _logger.Log("Server stopped"); //
        }
    }
}