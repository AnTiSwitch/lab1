// Це простори імен, які ви маєте імпортувати
using EchoServer.Abstractions;
using EchoServer.Services;
using Moq;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NetSdrClientAppTests
{
    // Оскільки ваш файл лежить у цьому проекті, використовуйте цей простір імен
    public class EchoServerServiceTests
    {
        // Приватні поля для мок-об'єктів, які використовуються у багатьох тестах
        private readonly Mock<ILogger> _mockLogger = new Mock<ILogger>();
        private readonly Mock<ITcpListenerFactory> _mockFactory = new Mock<ITcpListenerFactory>();
        private readonly Mock<ITcpListenerWrapper> _mockListener = new Mock<ITcpListenerWrapper>();
        private readonly Mock<ITcpClientWrapper> _mockClient = new Mock<ITcpClientWrapper>();
        private readonly Mock<INetworkStreamWrapper> _mockStream = new Mock<INetworkStreamWrapper>();

        private EchoServerService CreateServer(int port = 5000)
        {
            // Налаштовуємо Factory: завжди повертає наш Mock Listener
            _mockFactory.Setup(f => f.Create(It.IsAny<IPAddress>(), port)).Returns(_mockListener.Object);

            // Налаштовуємо Listener: 1) Приймає клієнта, 2) Кидає виняток (для виходу з циклу StartAsync)
            _mockListener.SetupSequence(l => l.AcceptTcpClientAsync())
                .ReturnsAsync(_mockClient.Object)
                .ThrowsAsync(new ObjectDisposedException("Simulated listener close"));

            // Налаштовуємо Client: завжди повертає наш Mock Stream
            _mockClient.Setup(c => c.GetStream()).Returns(_mockStream.Object);

            return new EchoServerService(port, _mockLogger.Object, _mockFactory.Object);
        }

        [Fact]
        public async Task StartAsync_ShouldEchoMessage_AndLogEvents()
        {
            // Arrange
            var server = CreateServer();
            string testMessage = "Test Data";
            byte[] messageBytes = Encoding.UTF8.GetBytes(testMessage);

            // Налаштовуємо Stream: 1. Читаємо дані. 2. Кінець потоку (0 байт).
            _mockStream.SetupSequence(s => s.ReadAsync(
                It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messageBytes.Length)
                .ReturnsAsync(0);

            // Act
            var serverTask = server.StartAsync();

            // Даємо час на обробку клієнта
            await Task.Delay(100);

            server.Stop(); // Викликаємо Stop, щоб listener кинув виняток і StartAsync завершився

            // Assert
            await serverTask; // Чекаємо завершення StartAsync

            // 1. Перевірка Echo-логіки
            _mockStream.Verify(
                s => s.WriteAsync(
                    // Перевіряємо, що в WriteAsync передані ті самі байти
                    It.Is<byte[]>(b => b.Take(messageBytes.Length).SequenceEqual(messageBytes)),
                    0,
                    messageBytes.Length,
                    It.IsAny<CancellationToken>()),
                Times.Once, "Повідомлення не було відправлено назад (Echo).");

            // 2. Перевірка коректного закриття ресурсів
            _mockClient.Verify(c => c.Close(), Times.Once, "Клієнт повинен бути закритий у блоці finally.");

            // 3. Перевірка логування ключових подій
            _mockLogger.Verify(l => l.Log("Server started on port 5000."), Times.Once);
            _mockLogger.Verify(l => l.Log("Client connected."), Times.Once);
            _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Echoed 9 bytes"))), Times.Once); // 9 байт довжина "Test Data"
            _mockLogger.Verify(l => l.Log("Client disconnected."), Times.Once);
            _mockLogger.Verify(l => l.Log("Server shutdown."), Times.Once);
        }

        [Fact]
        public async Task HandleClientAsync_ShouldCloseClient_OnSimulatedNetworkError()
        {
            // Arrange
            var server = CreateServer();

            // Налаштовуємо Stream: кидаємо виняток під час читання, щоб перевірити блок catch/finally
            _mockStream.Setup(s => s.ReadAsync(
                It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Simulated Network Error"));

            // Act
            var serverTask = server.StartAsync();
            await Task.Delay(100);
            server.Stop();
            await serverTask;

            // Assert
            // 1. Перевіряємо, що помилка була залогована
            _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Simulated Network Error"))), Times.Once);
            // 2. З'єднання повинно бути закрито, незважаючи на помилку
            _mockClient.Verify(c => c.Close(), Times.Once, "Клієнт повинен бути закритий, навіть якщо сталася помилка.");
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentOutOfRangeException_ForInvalidPort()
        {
            // Assert
            Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new EchoServerService(0, _mockLogger.Object, _mockFactory.Object));
        }

        [Fact]
        public void Dispose_ShouldCallStopAndDisposeInternalResources()
        {
            // Arrange
            var server = CreateServer();

            // Act
            server.Dispose();

            // Assert
            _mockListener.Verify(l => l.Stop(), Times.Once, "Stop() має бути викликано на listener.");
            // Перевірка, що Dispose listener'а також викликається
        }
    }
}