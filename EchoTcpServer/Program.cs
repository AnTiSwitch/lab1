using System;
using System.Threading.Tasks;
using EchoServerImplementations;
using EchoServerServices;

// Program (статичний клас) - Точка входу в програму, оркестрація
public static class Program
{
    public static async Task Main(string[] args) //
    {
        [cite_start]// 1. Створює всі необхідні об'єкти (Composition Root) [cite: 213, 499]
        var logger = new ConsoleLogger();
        var factory = new TcpListenerFactory();

        [cite_start]// 2. Ініціалізація сервісів (Dependency Injection) [cite: 195]
        var server = new EchoServerService(5000, logger, factory);
        var sender = new UdpTimedSender("127.0.0.1", 60000, logger);

        try
        {
            [cite_start]// 3. Запуск ТСР сервера в окремому потоці [cite: 500, 510]
            var serverTask = Task.Run(() => server.StartAsync());

            [cite_start]// 4. Запускає UDP відправник [cite: 501, 512]
            sender.StartSending(5000); // Інтервал 5 сек

            logger.Log("Press 'q' to gracefully shut down the server."); //

            [cite_start]// 5. Чекає на натискання клавіші 'q' для завершення [cite: 502]
            while (Console.ReadKey(true).Key != ConsoleKey.Q)
            {
                // Wait for 'q'
            }

            [cite_start]// 6. Зупиняє всі сервіси при виході [cite: 503]
            logger.Log("Stopping services...");
            sender.StopSending();
            server.Stop();

            await serverTask; // Чекаємо завершення роботи сервера
        }
        catch (Exception ex)
        {
            logger.LogError($"Application failed: {ex.Message}");
        }
        finally
        {
            sender.Dispose();
        }
    }
}