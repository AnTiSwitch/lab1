using NUnit.Framework;
using EchoServer.Implementations;
using System.Net;
using EchoServer.Abstractions;
using System.Net.Sockets;
using System.Threading.Tasks;
using static NUnit.Framework.Assert;
using static NUnit.Framework.Is; // Статичний імпорт для Is.Not.Null, Is.InstanceOf
using static NUnit.Framework.Throws; // Статичний імпорт для Throws.Nothing

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

            // Використовуємо Assert.That(value, Is.Not.Null)
            Assert.That(listenerField.GetValue(wrapper), Is.Not.Null);
            wrapper.Dispose();
        }

        [Test]
        public void Dispose_CallsStopOnInternalListener()
        {
            var wrapper = new TcpListenerWrapper(IPAddress.Loopback, 5000);
            wrapper.Start();
            wrapper.Dispose();

            // ВИПРАВЛЕННЯ: Використовуємо Assert.That з Throws.Nothing
            Assert.That(() =>
            {
                var newWrapper = new TcpListenerWrapper(IPAddress.Loopback, 5000);
                newWrapper.Dispose();
            }, Is.Not.Throwing);
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

            // Використовуємо Assert.That та Is.Not.Null / Is.InstanceOf
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<TcpClientWrapper>());

            wrapper.Dispose();
            result.Dispose();
        }
    }
}