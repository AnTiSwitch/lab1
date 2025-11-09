using NUnit.Framework;
using Moq;
using EchoTcpServer;
using EchoTcpServer.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using EchoTcpServer.Wrappers;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class UdpTimedSenderTests
    {
        private Mock<ILogger> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger>();
        }

        // Цей тест складний, оскільки UdpTimedSender створює UdpClient та Timer всередині.
        // Для повного юніт-тестування (якщо ви хочете досягти 95 тестів), 
        // потрібно було б обгорнути UdpClient та Timer в інтерфейси (аналогічно ТСР).

        // --- Тест з ILogger (можливий) ---

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Перевіряємо, що конструктор UdpTimedSender валідує логер
            Assert.Throws<ArgumentNullException>(() => new UdpTimedSender("127.0.0.1", 60000, null));
        }

        [Test]
        public void StartSending_ShouldThrowException_WhenAlreadyRunning()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60000, _mockLogger.Object);

            // Примусово створюємо Timer через рефлексію для тестування
            // Пропускаємо цей крок, оскільки він занадто залежить від internals.

            // Замість цього, тестуємо, що логіка StartSending працює:
            sender.StartSending(100);
            // Якщо викликати StartSending знову, воно має кинути виняток, 
            // але це вимагає імітації internal стану.

            Assert.Throws<InvalidOperationException>(() => sender.StartSending(100),
                "Sender should throw when StartSending is called twice.");

            sender.Dispose();
        }

    }
}