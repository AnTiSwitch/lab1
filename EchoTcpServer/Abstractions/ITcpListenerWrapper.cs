using System;
using System.Threading.Tasks;
using EchoServer.Abstractions;

namespace EchoServer.Abstractions
{
    // Обгортка для TcpListener
    public interface ITcpListenerWrapper : IDisposable
    {
        void Start();
        void Stop();
        Task<ITcpClientWrapper> AcceptTcpClientAsync();
    }
}