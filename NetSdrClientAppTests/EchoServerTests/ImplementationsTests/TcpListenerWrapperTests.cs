using NUnit.Framework;
using EchoServer.Implementations;
using System.Net;
using EchoServer.Abstractions;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework.Constraints;
using static NUnit.Framework.Assert;

namespace NetSdrClientAppTests.EchoServerTests.ImplementationsTests
{
    [TestFixture]
    public class TcpListenerWrapperTests
    {
        [Test]
        public void Constructor_CreatesInternalTcpListener()
        {
            var wrapper = new TcpListenerWrapper(IPAddress.Loopback, 5000);
            var listenerField = wrapper.GetType().GetField("_listener", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.That(listenerField.GetValue(wrapper), Is.Not.Null);
            wrapper.Dispose();
        }

        [Test]
        public void Dispose_CallsStopOnInternalListener()
        {
            var wrapper = new TcpListenerWrapper(IPAddress.Loopback, 5000);
            wrapper.Start();
            wrapper.Dispose();

            Assert.That(() =>
            {
                var newWrapper = new TcpListenerWrapper(IPAddress.Loopback, 5000);
                newWrapper.Dispose();
            }, Throws.Nothing);
        }

        [Test]
        public async Task AcceptTcpClientAsync_WhenClientConnects_ReturnsTcpClientWrapper()
        {
            const int testPort = 5001;
            var wrapper = new TcpListenerWrapper(IPAddress.Loopback, testPort);
            wrapper.Start();

            var acceptTask = wrapper.AcceptTcpClientAsync();
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, testPort);
            }

            ITcpClientWrapper result = await acceptTask;

            IsNotNull(result);
            IsInstanceOf<TcpClientWrapper>(result);

            wrapper.Dispose();
            result.Dispose();
        }
    }
}