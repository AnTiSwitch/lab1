using System;

namespace EchoServer.Abstractions
{
    // Інтерфейс для логування, абстрагує Console.WriteLine
    public interface ILogger
    {
        void Log(string message);
        void LogError(string message);
    }
}