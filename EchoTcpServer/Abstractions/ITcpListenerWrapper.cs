using System.Threading.Tasks;

namespace EchoTcpServer.Abstractions
{
    public interface ITcpListenerWrapper
    {
        void Start();
        void Stop();
        Task<ITcpClientWrapper> AcceptTcpClientAsync();
    }
}