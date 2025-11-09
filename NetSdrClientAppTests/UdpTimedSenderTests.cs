using EchoServer;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class UdpTimedSenderTests
    {
        // Тест 1: Перевірка коректності формування пакета та виклику Send
        [Test]
        public void SendMessageCallback_ShouldSendCorrectDataOnce()
        {
            // Arrange
            var mockUdp = new Mock<IUdpClient>(MockBehavior.Strict);
            var mockRnd = new Mock<IRandomDataGenerator>();

            // Налаштовуємо "фейкові" дані, які має повернути генератор
            byte[] expectedSamples = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            mockRnd.Setup(r => r.GenerateBytes(It.IsAny<int>())).Returns(expectedSamples);
            var expectedEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 60000);

            // Налаштовуємо очікуваний виклик Send на Moq-об'єкті
            mockUdp.Setup(u => u.Send(
                It.Is<byte[]>(data => data.Skip(6).SequenceEqual(expectedSamples)), // Перевіряємо, що дані correct
                It.IsAny<int>(),
                It.IsAny<IPEndPoint>()
            )).Returns(expectedSamples.Length);

            var sender = new UdpTimedSender("127.0.0.1", 60000, mockUdp.Object, mockRnd.Object);

            // Act: Виклик приватного методу через рефлексію
            var method = typeof(UdpTimedSender).GetMethod("SendMessageCallback", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(sender, new object[] { null });

            // Assert
            mockUdp.Verify(u => u.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()), Times.Once);
        }

        // Тест 2: Перевірка обробки помилок (покриття блоку catch)
        [Test]
        public void SendMessageCallback_WhenSendFails_ShouldCatchAndContinue()
        {
            // Arrange
            var mockUdp = new Mock<IUdpClient>(MockBehavior.Strict);
            var mockRnd = new Mock<IRandomDataGenerator>();

            // Налаштовуємо на викидання винятку
            mockUdp.Setup(u => u.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<IPEndPoint>()))
               .Throws(new SocketException());
            mockRnd.Setup(r => r.GenerateBytes(It.IsAny<int>())).Returns(new byte[1024]);

            var sender = new UdpTimedSender("127.0.0.1", 60000, mockUdp.Object, mockRnd.Object);

            // Act & Assert
            var method = typeof(UdpTimedSender).GetMethod("SendMessageCallback", BindingFlags.NonPublic | BindingFlags.Instance);

            // Перевіряємо, що метод не кидає виняток і коректно обробляє помилку
            Assert.DoesNotThrow(() => method.Invoke(sender, new object[] { null }));
        }

        // Тест 3: Перевірка коректного Dispose
        [Test]
        public void Dispose_ShouldCallDisposeOnUdpClient()
        {
            // Arrange
            var mockUdp = new Mock<IUdpClient>();
            var mockRnd = new Mock<IRandomDataGenerator>();
            var sender = new UdpTimedSender("127.0.0.1", 1234, mockUdp.Object, mockRnd.Object);

            // Act
            sender.Dispose();

            // Assert
            mockUdp.Verify(u => u.Dispose(), Times.Once);
        }
    }
}