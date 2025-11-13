using NUnit.Framework;
using Moq;
using EchoServer;
using EchoServer.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Reflection;

namespace NetSdrClientAppTests.EchoServerTests.ServiceTests
{
    [TestFixture]
    public class EchoServerServiceTests
    {
        private Mock<ILogger> _mockLogger;
        private Mock<ITcpListenerFactory> _mockFactory;
        private Mock<ITcpListenerWrapper> _mockListener;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger>();
            _mockFactory = new Mock<ITcpListenerFactory>();
            _mockListener = new Mock<ITcpListenerWrapper>();
            _mockFactory.Setup(f => f.Create(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>())).Returns(_mockListener.Object);
        }

        [Test]
        public void Constructor_NullDependencies_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EchoServerService(5000, null!, _mockFactory.Object));
            Assert.Throws<ArgumentNullException>(() => new EchoServerService(5000, _mockLogger.Object, null!));
        }

        [Test]
        public async Task StartAsync_ValidDependencies_CallsFactoryAndListenerStart()
        {
            var server = new EchoServerService(5000, _mockLogger.Object, _mockFactory.Object);
            _mockListener.Setup(l => l.AcceptTcpClientAsync()).ThrowsAsync(new OperationCanceledException());

            await server.StartAsync();

            // Перевіряємо виклики через Moq.Verify (Verify повертає void)
            _mockFactory.Verify(f => f.Create(It.IsAny<System.Net.IPAddress>(), 5000), Times.Once);
            _mockListener.Verify(l => l.Start(), Times.Once);
            _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Server started on port 5000"))), Times.Once, "Запуск сервера не був залогований.");
        }

        [Test]
        public async Task StartAsync_WhenStopCalled_LogsGracefulShutdown()
        {
            var server = new EchoServerService(5000, _mockLogger.Object, _mockFactory.Object);

            _mockListener.Setup(l => l.AcceptTcpClientAsync()).Returns(async () =>
            {
                await Task.Delay(100);
                throw new OperationCanceledException();
            });

            var startTask = server.StartAsync();
            server.Stop();
            await startTask;

            _mockLogger.Verify(l => l.Log("Server shut down gracefully."), Times.Once, "Коректна зупинка не була залогована.");
            _mockListener.Verify(l => l.Stop(), Times.Once, "Stop() на Listener не був викликаний.");
        }

        [Test]
        public async Task HandleClientAsync_ReceivesData_ShouldEchoItBack()
        {
            const string testMessage = "TEST_ECHO_DATA";
            var inputBytes = Encoding.UTF8.GetBytes(testMessage);
            const int bytesLength = 14;

            var mockStream = new Mock<INetworkStreamWrapper>();
            var mockClient = new Mock<ITcpClientWrapper>();

            mockStream.SetupSequence(s => s.ReadAsync(
                It.IsAny<byte[]>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
                .Returns(async (byte[] buffer, int offset, int count, CancellationToken token) =>
                {
                    Array.Copy(inputBytes, 0, buffer, offset, bytesLength);
                    await Task.Yield();
                    return bytesLength;
                })
                .ReturnsAsync(0);

            mockClient.Setup(c => c.GetStream()).Returns(mockStream.Object);
            var server = new EchoServerService(5000, _mockLogger.Object, _mockFactory.Object);

            MethodInfo handleClientMethod = server.GetType().GetMethod("HandleClientAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

            await (Task)handleClientMethod.Invoke(server, new object[] { mockClient.Object, CancellationToken.None })!;

            mockStream.Verify(
                s => s.WriteAsync(
                    It.Is<byte[]>(b => Encoding.UTF8.GetString(b, 0, bytesLength) == testMessage),
                    It.IsAny<int>(),
                    It.Is<int>(c => c == bytesLength),
                    It.IsAny<CancellationToken>()),
                Times.Once,
                "Логіка Echo не спрацювала: WriteAsync не викликано з коректними даними."
            );

            mockClient.Verify(c => c.Close(), Times.Once, "З'єднання має бути закрито після завершення обробки.");
            _mockLogger.Verify(l => l.Log("Client disconnected."), Times.Once, "Відключення клієнта має бути залоговано.");
        }

        [Test]
        public async Task HandleClientAsync_ClientDisconnectsImmediately_ShouldCloseClient()
        {
            var mockStream = new Mock<INetworkStreamWrapper>();
            var mockClient = new Mock<ITcpClientWrapper>();

            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            mockClient.Setup(c => c.GetStream()).Returns(mockStream.Object);
            var server = new EchoServerService(5000, _mockLogger.Object, _mockFactory.Object);

            MethodInfo handleClientMethod = server.GetType().GetMethod("HandleClientAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)handleClientMethod.Invoke(server, new object[] { mockClient.Object, CancellationToken.None });

            mockClient.Verify(c => c.Close(), Times.Once, "Клієнт повинен бути закритий, якщо ReadAsync повернув 0.");
            mockStream.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never, "WriteAsync не повинен був викликатися.");
        }

        [Test]
        public async Task HandleClientAsync_StreamThrowsException_ShouldLogError()
        {
            var mockStream = new Mock<INetworkStreamWrapper>();
            var mockClient = new Mock<ITcpClientWrapper>();

            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test Network Error"));

            mockClient.Setup(c => c.GetStream()).Returns(mockStream.Object);
            var server = new EchoServerService(5000, _mockLogger.Object, _mockFactory.Object);

            MethodInfo handleClientMethod = server.GetType().GetMethod("HandleClientAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)handleClientMethod.Invoke(server, new object[] { mockClient.Object, CancellationToken.None });

            _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Client handler error: Test Network Error"))), Times.Once, "Виняток HandleClientAsync не був залогований як помилка.");
            mockClient.Verify(c => c.Close(), Times.Once, "З'єднання має бути закрито навіть після помилки.");
        }

        [Test]
        public async Task HandleClientAsync_MultipleReadsRequired_ShouldEchoAllData()
        {
            const string messagePart1 = "Part1";
            const string messagePart2 = "Part2";
            var bytes1 = Encoding.UTF8.GetBytes(messagePart1);
            var bytes2 = Encoding.UTF8.GetBytes(messagePart2);

            var mockStream = new Mock<INetworkStreamWrapper>();
            var mockClient = new Mock<ITcpClientWrapper>();

            mockStream.SetupSequence(s => s.ReadAsync(
                It.IsAny<byte[]>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
                .Returns(async (byte[] buffer, int offset, int count, CancellationToken token) =>
                {
                    Array.Copy(bytes1, 0, buffer, offset, bytes1.Length);
                    await Task.Yield();
                    return bytes1.Length;
                })
                .Returns(async (byte[] buffer, int offset, int count, CancellationToken token) =>
                {
                    Array.Copy(bytes2, 0, buffer, offset, bytes2.Length);
                    await Task.Yield();
                    return bytes2.Length;
                })
                .ReturnsAsync(0);

            mockClient.Setup(c => c.GetStream()).Returns(mockStream.Object);
            var server = new EchoServerService(5000, _mockLogger.Object, _mockFactory.Object);

            MethodInfo handleClientMethod = server.GetType().GetMethod("HandleClientAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)handleClientMethod.Invoke(server, new object[] { mockClient.Object, CancellationToken.None });

            mockStream.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2), "WriteAsync має бути викликаний для кожної частини даних.");

            _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains(messagePart1))), Times.Once, "Перша частина не залогована.");
            _mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains(messagePart2))), Times.Once, "Друга частина не залогована.");
        }
    }
}
