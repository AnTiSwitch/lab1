using System;
using EchoServer.Abstractions;

namespace EchoServer.Implementations
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }


        public void LogError(string message)
        {
            // Можна додати колір, як описано в аналізі
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {message}");
            Console.ResetColor();
        }
    }
}