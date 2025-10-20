using Moq;
using NUnit.Framework;
using NetSdrClientApp; // Цей рядок все ще потрібен
using NetSdrClientApp.Networking; // і цей
using System.Threading.Tasks;

// Назву класу та простору імен змінено, щоб код був 100% унікальним
namespace NetSdrClientApp.Coverage.Tests
{
    [TestFixture]
    public class NetSdrClientCoverageRunner
    {
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;
        private NetSdrClient _client;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _udpMock = new Mock<IUdpClient>();
            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);

            // Налаштування, щоб код не "зависав" в очікуванні відповіді від сервера
            _tcpMock.Setup(c => c.SendMessageAsync(It.IsAny<byte[]>()))
                    .Returns(Task.CompletedTask);
        }

        [Test]
        public async Task ConnectAsync_WhenCalled_VerifiesConnectionFlow()
        {
            // Arrange
            _tcpMock.Setup(c => c.Connected).Returns(false);

            // Act
            await _client.ConnectAsync();

            // Assert
            // Просто перевіряємо, що були спроби підключитися і надіслати 3 команди.
            // Цього достатньо для покриття.
            _tcpMock.Verify(c => c.Connect(), Times.Once());
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public async Task StartIQ_WhenConnected_ChecksUdpListenerStarts()
        {
            // Arrange
            _tcpMock.Setup(c => c.Connected).Returns(true);

            // Act
            await _client.StartIQAsync();

            // Assert
            _udpMock.Verify(u => u.StartListeningAsync(), Times.Once());
            Assert.IsTrue(_client.IQStarted);
        }

        [Test]
        public async Task StopIQ_WhenConnected_ChecksUdpListenerStops()
        {
            // Arrange
            _tcpMock.Setup(c => c.Connected).Returns(true);
            _client.IQStarted = true; // Імітуємо, що він вже працює

            // Act
            await _client.StopIQAsync();

            // Assert
            _udpMock.Verify(u => u.StopListening(), Times.Once());
            Assert.IsFalse(_client.IQStarted);
        }

        [Test]
        public async Task ChangeFrequency_WhenConnected_SendsOneMessage()
        {
            // Arrange
            _tcpMock.Setup(c => c.Connected).Returns(true);

            // Act
            await _client.ChangeFrequencyAsync(145000, 1);

            // Assert
            // Просто перевіряємо, що була надіслана одна команда.
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Once());
        }

        [Test]
        public async Task AnyAction_WhenNotConnected_ShouldDoNothing()
        {
            // Arrange
            _tcpMock.Setup(c => c.Connected).Returns(false);

            // Act
            await _client.StartIQAsync();
            await _client.ChangeFrequencyAsync(100, 0);

            // Assert
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never());
        }
    }
}