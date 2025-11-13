using System;
using EchoServerAbstractions;

namespace EchoServerImplementations
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message); //
        }

        public void LogError(string message)
        {
            Console.WriteLine($"ERROR: {message}"); //
        }
    }
}