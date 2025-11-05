using System;
using System.Net;
using System.Threading.Tasks;
using EchoServer.Implementations;
using EchoServer.Abstractions;
using EchoServer.Services;
using System.Threading;

namespace EchoServer
{
    public static class Program 
    {
        public static void Main(string[] args) 
        {
            // 1. Створення залежностей (Composition Root)
            ILogger logger = new ConsoleLogger();
            ITcpListenerFactory factory = new TcpListenerFactory();

            int tcpPort = 5000;

            // 2. Створення сервісів з DI
            using (EchoServerService server = new EchoServerService(tcpPort, logger, factory))
            {
                // Запуск сервера в окремому завданні
                _ = Task.Run(() => server.StartAsync());

                string host = "127.0.0.1";
                int udpPort = 60000;
                int intervalMilliseconds = 5000;

                using (var sender = new UdpTimedSender(host, udpPort, logger))
                {
                    logger.Log("Press 'q' to quit...");
                    sender.StartSending(intervalMilliseconds);

                    // Обробка користувацького вводу
                    while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
                    {
                        // Просто чекаємо 'q'
                    }

                    // Graceful shutdown
                    sender.StopSending();
                    server.Stop();

                 

                    logger.Log("Application stopped.");
                }
            }
            Thread.Sleep(100);
        }
    }
}