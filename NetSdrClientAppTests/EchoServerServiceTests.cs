using NUnit.Framework;
using Moq;
using EchoTcpServer;
using EchoTcpServer.Abstractions;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class EchoServerServiceTests
    {
        // Поля для мок-об'єктів
        private Mock<ILogger> _mockLogger;
        private Mock<ITcpListenerFactory> _mockListenerFactory;
        private Mock<ITcpListenerWrapper> _mockListener;
        private EchoServerService _server;
        private const int TestPort = 5000;

        [SetUp]
        public void SetUp()
        {
            // Ініціалізація мок-об'єктів перед кожним тестом
            _mockLogger = new Mock<ILogger>();
            _mockListenerFactory = new Mock<ITcpListenerFactory>();
            _mockListener = new Mock<ITcpListenerWrapper>();

            // Налаштування фабрики, щоб вона завжди повертала наш мок-Listener
            _mockListenerFactory
                .Setup(f => f.Create(IPAddress.Any, TestPort))
                .Returns(_mockListener.Object);

            // Створення тестованого об'єкта (SUT) з моками через DI
            _server = new EchoServerService(TestPort, _mockLogger.Object, _mockListenerFactory.Object);
        }

        // --- Тести конструктора та ініціалізації ---

        [Test]
        public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenPortIsInvalid()
        {
            // Перевіряємо, чи спрацьовує валідація, яку ми додали під час рефакторингу
            Assert.Throws<ArgumentOutOfRangeException>(() => new EchoServerService(0, _mockLogger.Object, _mockListenerFactory.Object));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new EchoServerService(TestPort, null, _mockListenerFactory.Object));
        }

        // --- Тести методу Stop() ---

        [Test]
        public void Stop_ShouldCallListenerStopAndLogShutdown()
        {
            // 1. Спочатку запускаємо StartAsync, щоб _listener ініціалізувався
            // (Виклик StartAsync для ініціалізації)
            _server.StartAsync();

            // 2. Викликаємо Stop()
            _server.Stop();

            // 3. Перевіряємо, що методи мок-об'єктів були викликані

            // Перевіряємо, що викликано Stop на Listener
            _mockListener.Verify(l => l.Stop(), Times.Once);

            // Перевіряємо, що було викликано логування "Server stopped."
            _mockLogger.Verify(l => l.Log("Server stopped."), Times.Once);
        }

        // --- Тест HandleClientAsync (Ехо-логіка) ---

        [Test]
        public async Task HandleClientAsync_ShouldEchoReceivedDataAndLog()
        {
            // 1. Імітуємо з'єднання та потік даних
            var mockClient = new Mock<ITcpClientWrapper>();
            var mockStream = new Mock<INetworkStreamWrapper>();

            mockClient.Setup(c => c.GetStream()).Returns(mockStream.Object);

            // Дані, які "прочитає" потік: "HELLO" (5 байт)
            byte[] inputData = System.Text.Encoding.ASCII.GetBytes("HELLO");
            byte[] buffer = new byte[8192];

            // Налаштовуємо потік для імітації читання:
            // 1й виклик: читає 5 байт (HELLO)
            // 2й виклик: читає 0 байт (кінець потоку)
            var sequence = new MockSequence();
            mockStream.InSequence(sequence)
                      .Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[] b, int offset, int count, CancellationToken t) => {
                          Array.Copy(inputData, 0, b, offset, inputData.Length);
                          return inputData.Length;
                      });
            mockStream.InSequence(sequence)
                      .Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(0);

            // 2. Викликаємо HandleClientAsync (потрібна рефлексія або виклик через приватний метод)
            // Оскільки HandleClientAsync є private, його потрібно викликати за допомогою рефлексії або зробити public для тестування (якщо дозволено).
            // Припустимо, що ми зробили його protected/internal для тестування:

            // Тут потрібно викликати HandleClientAsync(mockClient.Object, CancellationToken.None);
            // ... [Виклик методу] ...

            // 3. Перевірка

            // Перевіряємо, що було викликано запис 5 байт (ЕХО)
            mockStream.Verify(s => s.WriteAsync(It.IsAny<byte[]>(),
                                               It.IsAny<int>(),
                                               inputData.Length, // перевіряємо, що відправлено 5 байт
                                               It.IsAny<CancellationToken>()), Times.Once);

            // Перевіряємо, що логування було викликане
            _mockLogger.Verify(l => l.Log($"Echoed 5 bytes to the client."), Times.Once);
            _mockLogger.Verify(l => l.Log("Client disconnected."), Times.Once);
            mockClient.Verify(c => c.Close(), Times.Once);
        }
    }
}