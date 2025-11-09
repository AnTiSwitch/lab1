using EchoTcpServer.Abstractions;
using EchoTcpServer.Wrappers; // Для використання ConsoleLogger та TcpListenerFactory
using System;
using System.Threading.Tasks;

namespace EchoTcpServer
{
    // Клас має бути статичним, відповідає лише за запуск
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // --- Composition Root (Збирання залежностей) ---

            // Створюємо конкретні реалізації
            ILogger logger = new ConsoleLogger();
            ITcpListenerFactory factory = new TcpListenerFactory();

            // Налаштування для UDP
            string host = "127.0.0.1";
            int port = 60000;
            int intervalMilliseconds = 5000;

            // Створення сервісів
            EchoServerService server = new EchoServerService(5000, logger, factory);

            // Тепер UdpTimedSender також отримує logger
            using (var sender = new UdpTimedSender(host, port, logger))
            {
                // --- Запуск та керування ---

                // Start the server in a separate task
                _ = Task.Run(() => server.StartAsync());

                logger.Log("Press any key to stop sending...");
                sender.StartSending(intervalMilliseconds);

                logger.Log("Press 'q' to quit...");
                while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
                {
                    // Just wait until 'q' is pressed
                }

                // Зупинка сервісів
                sender.StopSending();
                server.Stop();
                logger.Log("Sender stopped.");
            }
        }
    }
}