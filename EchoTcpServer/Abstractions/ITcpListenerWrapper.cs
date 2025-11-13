using System.Threading.Tasks;

namespace EchoServerAbstractions
{
    public interface ITcpListenerWrapper
    {
        void Start(); //
        void Stop(); //
        Task<ITcpClientWrapper> AcceptTcpClientAsync(); // Повертає ITcpClientWrapper
    }
}