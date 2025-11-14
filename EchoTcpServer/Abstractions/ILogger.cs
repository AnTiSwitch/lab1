using System;

namespace EchoServer.Abstractions
{
    // ��������� ��� ���������, �������� Console.WriteLine
    public interface ILogger
    {
        void Log(string message);
        void LogError(string message);
    }
}