using EchoTcpServer.Abstractions;
using System;

namespace EchoTcpServer.Wrappers
{
    // Реалізація логування, що використовує Console
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void LogError(string message)
        {
            Console.WriteLine($"ERROR: {message}");
        }
    }
}