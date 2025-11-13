using System;
using EchoServer.Abstractions;

namespace EchoServer.Implementations
{
    // Реалізація логування через консоль
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} {message}");
        }

        public void LogError(string message)
        {
            // Виведення помилок червоним кольором
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {message}");
            Console.ForegroundColor = originalColor;
        }
    }
}