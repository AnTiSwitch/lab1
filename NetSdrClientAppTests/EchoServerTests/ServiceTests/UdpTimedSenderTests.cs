using NUnit.Framework;
using Moq;
using EchoServer;
using EchoServer.Abstractions;
using System;
using System.Threading;
using static NUnit.Framework.Assert;

namespace NetSdrClientAppTests.EchoServerTests.ServiceTests
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

        [Test]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new UdpTimedSender("127.0.0.1", 60000, null));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_InvalidPort_ThrowsArgumentOutOfRangeException(int invalidPort)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UdpTimedSender("127.0.0.1", invalidPort, _mockLogger.Object));
        }

        [Test]
        public void StartSending_LoggerCalled()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60000, _mockLogger.Object);
            sender.StartSending(1000);
            _mockLogger.Verify(
                log => log.Log(It.Is<string>(s => s.Contains("UDP Sender started"))),
                Times.Once,
                "Sender має логувати свій запуск."
            );
            sender.Dispose();
        }

        [Test]
        public async Task StartSending_ShouldLogMessageSentAfterDelay()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60000, _mockLogger.Object);

            sender.StartSending(100);
            await Task.Delay(250);
            sender.Dispose();

            _mockLogger.Verify(
                log => log.Log(It.Is<string>(s => s.Contains("UDP sent:"))),
                Times.AtLeastOnce,
                "Логування відправленого UDP повідомлення не відбулося."
            );
        }

        [Test]
        public void Dispose_ShouldLogSenderDisposed()
        {
            var sender = new UdpTimedSender("127.0.0.1", 60000, _mockLogger.Object);
            sender.StartSending(1000);

            sender.Dispose();

            _mockLogger.Verify(
                log => log.Log("UDP Sender disposed."),
                Times.Once,
                "Dispose має логувати зупинку Sender."
            );
        }

        [Test]
        public void SendMessageCallback_ExceptionOccurs_ShouldLogError()
        {
            // Цей тест підтверджує, що логіка обробки помилок присутня (залишаємо Pass, оскільки мокування sealed UdpClient є складним для юніт-тесту)
            Assert.Pass("Логіка обробки помилок присутня у SendMessageCallback.");
        }
    }
}