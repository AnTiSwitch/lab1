using System;

namespace EchoTcpServer.Abstractions
{
    public interface ILogger
    {
        void Log(string message);
        void LogError(string message);
    }
}