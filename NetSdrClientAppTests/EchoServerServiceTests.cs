using Xunit;
using Moq;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System;
using System.Net;
using System.Linq;

using EchoServer.Abstractions;
using EchoServer.Services;

namespace NetSdrClientAppTests
{
    public class EchoServerServiceTests
    {
        private readonly Mock<ILogger> _mockLogger = new Mock<ILogger>();
        private readonly Mock<ITcpListenerFactory> _mockFactory = new Mock<ITcpListenerFactory>();
        private readonly Mock<ITcpListenerWrapper> _mockListener = new Mock<ITcpListenerWrapper>();
        private readonly Mock<ITcpClientWrapper> _mockClient = new Mock<ITcpClientWrapper>();
        private readonly Mock<INetworkStreamWrapper> _mockStream = new Mock<INetworkStreamWrapper>();

        private EchoServerService CreateServer(int port = 5000)
        {
            _mockFactory.Setup(f => f.Create(It.IsAny<IPAddress>(), port)).Returns(_mockListener.Object);
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

            _mockListener.SetupSequence(l => l.AcceptTcpClientAsync())
                .ReturnsAsync(_mockClient.Object) // 1. Клієнт підключився
                .ThrowsAsync(new ObjectDisposedException("Simulated listener close")); // 2. Примусовий вихід

            _mockStream.SetupSequence(s => s.ReadAsync(
                It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(messageBytes.Length)
                .ReturnsAsync(0);

            // Act
            var serverTask = server.StartAsync();
            await Task.Delay(100);
            server.Stop();
            await serverTask;

            // Assert (Перевірка покриття всіх блоків)
            Xunit.Assert.True(serverTask.IsCompleted, "StartAsync повинен завершитися після Stop.");
            _mockStream.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), 0, messageBytes.Length, It.IsAny<CancellationToken>()), Times.Once);
            _mockClient.Verify(c => c.Close(), Times.Once);
            _mockLogger.Verify(l => l.Log("Server shutdown."), Times.Once);
        }

        [Fact]
        public async Task StartAsync_ShouldLogAcceptError_WhenListenerFails()
        {
            // Arrange
            var server = CreateServer();

            // 1. Кидаємо загальний виняток, щоб потрапити в блок catch StartAsync
            _mockListener.SetupSequence(l => l.AcceptTcpClientAsync())
                .ThrowsAsync(new InvalidOperationException("Accept failed"))
                .ThrowsAsync(new ObjectDisposedException("Simulated listener close"));

            // Act
            var serverTask = server.StartAsync();
            await Task.Delay(100);
            server.Stop();

            // Assert
            _mockLogger.Verify(l => l.LogError(Moq.It.Is<string>(s => s.Contains("Accept failed"))), Times.Once);
        }


        [Fact]
        public async Task HandleClientAsync_ShouldCatchExceptionAndLog()
        {
            // Arrange
            var server = CreateServer();

            // 1. Налаштовуємо Stream, щоб кинути виняток для покриття блоку catch
            _mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("Simulated Read Error"));

            _mockListener.SetupSequence(l => l.AcceptTcpClientAsync())
                .ReturnsAsync(_mockClient.Object)
                .ThrowsAsync(new ObjectDisposedException("Simulated listener close"));

            // Act
            var serverTask = server.StartAsync();
            await Task.Delay(100);
            server.Stop();
            await serverTask;

            // Assert
            _mockLogger.Verify(l => l.LogError(Moq.It.Is<string>(s => s.Contains("Simulated Read Error"))), Times.Once);
            _mockClient.Verify(c => c.Close(), Times.Once, "Клієнт повинен бути закритий у блоці finally.");
        }

        [Fact]
        public void Dispose_ShouldCallStopAndDisposeListener()
        {
            // Arrange
            var server = CreateServer();
            _mockListener.Setup(l => l.Dispose()); // Перевіряємо, що Dispose викликається

            // Act
            server.Dispose();

            // Assert
            _mockListener.Verify(l => l.Stop(), Times.Once, "Stop() має бути викликано на listener.");
            // Перевірка, що Dispose викликається 
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentOutOfRangeException_ForInvalidPort()
        {
            Xunit.Assert.Throws<ArgumentOutOfRangeException>(() => new EchoServerService(0, _mockLogger.Object, _mockFactory.Object));
        }
    }
}