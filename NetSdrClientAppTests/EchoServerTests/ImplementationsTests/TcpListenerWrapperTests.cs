using NUnit.Framework;
using EchoServer.Implementations;
using System.Net.Sockets;
using EchoServer.Abstractions;
using System.Threading.Tasks;
using System.Net;
using static NUnit.Framework.Assert;
using static NUnit.Framework.Is;

namespace NetSdrClientAppTests.EchoServerTests.ImplementationsTests
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        [Test]
        public void GetStream_ReturnsNetworkStreamWrapper()
        {
            // ВИПРАВЛЕННЯ: Створюємо мінімально підключений сокет
            using (var listener = new TcpListener(IPAddress.Loopback, 5004)) // Використовуємо інший порт
            {
                listener.Start();
                var connector = new TcpClient();
                var connectTask = connector.ConnectAsync(IPAddress.Loopback, 5004);

                // Приймаємо підключений клієнт
                TcpClient realClient = listener.AcceptTcpClient();

                var wrapper = new TcpClientWrapper(realClient);

                // Act
                INetworkStreamWrapper streamWrapper = wrapper.GetStream(); // Тепер не повинно бути InvalidOperationException

                // Assert
                Assert.That(streamWrapper, Is.Not.Null);
                Assert.That(streamWrapper, Is.InstanceOf<NetworkStreamWrapper>());

                // Cleanup
                wrapper.Dispose();
                connector.Dispose();
            }
        }

        [Test]
        public void CloseAndDispose_CalledOnInternalClient()
        {
            using (var listener = new TcpListener(IPAddress.Loopback, 5005)) // Використовуємо інший порт
            {
                listener.Start();
                var connectTask = Task.Run(() => new TcpClient("127.0.0.1", 5005));
                TcpClient realClient = listener.AcceptTcpClient();
                listener.Stop(); // Зупиняємо listener, але client залишається підключеним

                var wrapper = new TcpClientWrapper(realClient);

                wrapper.Close();

                // Перевіряємо, що внутрішній клієнт закритий
                Assert.That(realClient.Connected, Is.False, "Internal client should be closed after wrapper.Close()");

                wrapper.Dispose();
            }
        }
    }
}