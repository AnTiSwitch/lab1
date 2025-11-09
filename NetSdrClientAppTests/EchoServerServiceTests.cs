using NUnit.Framework;
using Moq;
using EchoTcpServer;
using EchoTcpServer.Abstractions;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Linq; // Для використання Linq в тестах

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

            // Налаштування фабрики: повертає наш мок-Listener
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
            // У реальному тесті ми не чекаємо його завершення
            _ = _server.StartAsync();

            _server.Stop();

            // Перевіряємо, що викликано Stop на Listener
            _mockListener.Verify(l => l.Stop(), Times.Once);

            // Перевіряємо, що викликано логування "Server stopped."
            _mockLogger.Verify(l => l.Log("Server stopped."), Times.Once);
        }

        // --- Тест HandleClientAsync (КЛЮЧОВИЙ ТЕСТ ЕХО-ЛОГІКИ) ---

        [Test]
        public async Task HandleClientAsync_ShouldEchoReceivedDataAndLog()
        {
            // 1. Імітуємо з'єднання та потік даних
            var mockClient = new Mock<ITcpClientWrapper>();
            var mockStream = new Mock<INetworkStreamWrapper>();

            mockClient.Setup(c => c.GetStream()).Returns(mockStream.Object);

            // Дані, які "прочитає" потік: "HELLO" (5 байт)
            byte[] inputData = Encoding.ASCII.GetBytes("HELLO");
            int bytesToReturn = inputData.Length; // 5 байт

            // Налаштовуємо послідовність викликів
            var sequence = new MockSequence();

            // 1й виклик ReadAsync: повертає 5 байт і КОПІЮЄ їх у буфер сервера
            mockStream.InSequence(sequence)
                      .Setup(s => s.ReadAsync(
                          It.IsAny<byte[]>(),
                          It.IsAny<int>(),
                          It.IsAny<int>(),
                          It.IsAny<CancellationToken>()))
                      // !!! Callback, який копіює дані в буфер сервера !!!
                      .Callback((byte[] buffer, int offset, int count, CancellationToken t) => {
                          Array.Copy(inputData, 0, buffer, offset, bytesToReturn);
                      })
                      .ReturnsAsync(bytesToReturn);

            // 2й виклик ReadAsync: повертає 0 (КІНЕЦЬ ПОТОКУ)
            mockStream.InSequence(sequence)
                      .Setup(s => s.ReadAsync(
                          It.IsAny<byte[]>(),
                          It.IsAny<int>(),
                          It.IsAny<int>(),
                          It.IsAny<CancellationToken>()))
                      .ReturnsAsync(0);

            // 2. Викликаємо HandleClientAsync через РЕФЛЕКСІЮ 
            var handleMethod = typeof(EchoServerService).GetMethod("HandleClientAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            // Запускаємо і очікуємо завершення
            var handlerTask = (Task)handleMethod.Invoke(_server, new object[] { mockClient.Object, CancellationToken.None });
            await handlerTask.ConfigureAwait(false);


            // 3. Перевірка

            // Перевіряємо, що було викликано запис 5 байт (ЕХО)
            mockStream.Verify(s => s.WriteAsync(
                                   It.IsAny<byte[]>(),
                                   It.IsAny<int>(),
                                   bytesToReturn, // 5 байт
                                   It.IsAny<CancellationToken>()), Times.Once);

            // Перевіряємо, що логування було викликане
            _mockLogger.Verify(l => l.Log($"Echoed {bytesToReturn} bytes to the client."), Times.Once);
            _mockLogger.Verify(l => l.Log("Client disconnected."), Times.Once);
            mockClient.Verify(c => c.Close(), Times.Once);
            mockStream.Verify(s => s.Dispose(), Times.Once); // Перевіряємо, що using спрацював
        }
    }
}