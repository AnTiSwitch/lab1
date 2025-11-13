using NUnit.Framework;
using NUnit.Framework.Constraints;
using EchoServer.Implementations;
using System.Net.Sockets;
using EchoServer.Abstractions;
using System.Threading.Tasks;
using System.Net;
using static NUnit.Framework.Assert;

namespace NetSdrClientAppTests.EchoServerTests.ImplementationsTests
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        [Test]
        public void GetStream_ReturnsNetworkStreamWrapper()
        {
            var realClient = new TcpClient();
            var wrapper = new TcpClientWrapper(realClient);
            INetworkStreamWrapper streamWrapper = wrapper.GetStream();

            Assert.That(streamWrapper, Is.Not.Null);
            Assert.That(streamWrapper, Is.InstanceOf<NetworkStreamWrapper>());
            wrapper.Dispose();
        }

        [Test]
        public void CloseAndDispose_CalledOnInternalClient()
        {
            using (var listener = new TcpListener(IPAddress.Loopback, 5002))
            {
                listener.Start();
                var connectTask = Task.Run(() => new TcpClient("127.0.0.1", 5002));
                var realClient = listener.AcceptTcpClient();
                listener.Stop();

                var wrapper = new TcpClientWrapper(realClient);

                wrapper.Close();

                Assert.That(realClient.Connected, Is.False, "Internal client should be closed after wrapper.Close()");

                wrapper.Dispose()
            }
        }
    }
}