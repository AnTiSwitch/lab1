using System;
using System.Net;
using System.Threading.Tasks;
using EchoServer.Abstractions;
using EchoServer.Implementations;

namespace EchoServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // === Composition Root: Створення об'єктів та впровадження залежностей ===

            // 1. Створення залежностей
            // Використовуємо конкретні реалізації (Impl)
            ILogger logger = new ConsoleLogger();
            ITcpListenerFactory factory = new TcpListenerFactory();

            // Порти за замовчуванням
            const int tcpPort = 5000;
            const int udpPort = 60000;

            // 2. Створення сервісів, передаючи залежності (DI)
            EchoServerService server = new EchoServerService(tcpPort, logger, factory);
            UdpTimedSender sender = new UdpTimedSender("127.0.0.1", udpPort, logger);

            // 3. Запуск сервісів
            // TCP server запускаємо у фоновому завданні
            Task serverTask = Task.Run(() => server.StartAsync());

            // UDP sender запускаємо з інтервалом 5000 мс (5 сек)
            sender.StartSending(5000);

            // 4. Очікування команди на завершення
            Console.WriteLine("Server is running. Press 'q' to quit.");

            // Блокуємо головний потік до натискання 'q'
            while (Console.ReadKey(true).Key != ConsoleKey.Q)
            {
                // Запобігання блокуванню процесора
                await Task.Delay(100);
            }

            // 5. Коректна зупинка
            Console.WriteLine("\nShutting down...");
            server.Stop(); // Викликаємо скасування токена та зупинку listener
            sender.Dispose(); // Зупиняємо таймер і закриваємо UDP клієнт

            // Очікуємо завершення роботи сервера
            await serverTask;

            logger.Log("Application exited.");
        }
    }
}