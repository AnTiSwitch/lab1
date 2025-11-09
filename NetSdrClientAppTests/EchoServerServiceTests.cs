using NUnit.Framework;
using Moq;
using EchoTcpServer;
using EchoTcpServer.Abstractions;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net;
using System.Reflection; // <--- ДОДАНО: Для виклику приватного методу

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
            _mockLogger = new Mock<ILogger>();
            _mockListenerFactory = new Mock<ITcpListenerFactory>();
            _mockListener = new Mock<ITcpListenerWrapper>();

            _mockListenerFactory
                .Setup(f => f.Create(IPAddress.Any, TestPort))
                .Returns(_mockListener.Object);

            _server = new EchoServerService(TestPort, _mockLogger.Object, _mockListenerFactory.Object);
        }

        // --- Тести конструктора та ініціалізації ---

        [Test]
        public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenPortIsInvalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EchoServerService(0, _mockLogger.Object, _mockListenerFactory.Object));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new EchoServerService(TestPort, null, _mockListenerFactory.Object));
        }

        // --- Тести методу Stop() ---

        [Test]
        public async Task Stop_ShouldCallListenerStopAndLogShutdown()
        {
            // Викликаємо StartAsync, щоб ініціалізувати _listener
            // (Виклик має бути awaitable, але ми можемо ігнорувати Task, якщо він не блокує)
            _server.StartAsync();

            _server.Stop();

            _mockListener.Verify(l => l.Stop(), Times.Once);
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
            int bytesToReturn = inputData.Length; // 5 байт

            // Налаштовуємо послідовність викликів
            var sequence = new MockSequence();

            // 1й виклик ReadAsync: повертає 5 байт і КОПІЮЄ їх у буфер сервера (ЧОМУ ПОМИЛКА БУЛА)
            mockStream.InSequence(sequence)
                      .Setup(s => s.ReadAsync(
                          It.IsAny<byte[]>(),
                          It.IsAny<int>(),
                          It.IsAny<int>(),
                          It.IsAny<CancellationToken>()))
                      // !!! КРИТИЧНО: Callback, який копіює дані в буфер !!!
                      .Callback((byte[] buffer, int offset, int count, CancellationToken t) => {
                          Array.Copy(inputData, 0, buffer, offset, bytesToReturn);
                      })
                      .ReturnsAsync(bytesToReturn); // Повертаємо 5 прочитаних байт

            // 2й виклик ReadAsync: повертає 0 (кінець потоку)
            mockStream.InSequence(sequence)
                      .Setup(s => s.ReadAsync(
                          It.IsAny<byte[]>(),
                          It.IsAny<int>(),
                          It.IsAny<int>(),
                          It.IsAny<CancellationToken>()))
                      .ReturnsAsync(0);

            // 2. Викликаємо HandleClientAsync через РЕФЛЕКСІЮ (оскільки він приватний)
            // Це стандартний хак для тестування private методів, якщо немає можливості змінити його на internal.
            var handleMethod = typeof(EchoServerService).GetMethod("HandleClientAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            await (Task)handleMethod.Invoke(_server, new object[] { mockClient.Object, CancellationToken.None });


            // 3. Перевірка

            // Перевіряємо, що було викликано запис 5 байт (ЕХО)
            mockStream.Verify(s => s.WriteAsync(
                                   It.IsAny<byte[]>(),
                                   It.IsAny<int>(),
                                   bytesToReturn, // перевіряємо, що відправлено 5 байт
                                   It.IsAny<CancellationToken>()), Times.Once);

            // Перевіряємо, що логування було викликане
            _mockLogger.Verify(l => l.Log($"Echoed {bytesToReturn} bytes to the client."), Times.Once);
            _mockLogger.Verify(l => l.Log("Client disconnected."), Times.Once);
            mockClient.Verify(c => c.Close(), Times.Once);
        }
    }
}