using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrClientTests
    {
        private NetSdrClient _client;
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _udpMock = new Mock<IUdpClient>();

            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);

            _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
            {
                _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            });

            _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
            {
                _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            });

            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
        }

        [Test]
        public async Task ConnectAsync_WhenNotConnected_ConnectsAndSendsThreeSetupMessages()
        {
            await _client.ConnectAsync();

            _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public async Task ConnectAsync_WhenAlreadyConnected_DoesNothing()
        {
            _tcpMock.Setup(c => c.Connected).Returns(true);

            await _client.ConnectAsync();

            _tcpMock.Verify(c => c.Connect(), Times.Never);
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public void Disconnect_WhenCalled_CallsTcpDisconnect()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);

            _client.Disconect();

            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        }

        [Test]
        public async Task StartIQAsync_WhenNotConnected_DoesNothing()
        {
            await _client.StartIQAsync();

            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            Assert.That(_client.IQStarted, Is.False);
        }

        [Test]
        public async Task StartIQAsync_WhenConnected_SendsMessageAndStartsUdp()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);

            await _client.StartIQAsync();

            _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
            Assert.That(_client.IQStarted, Is.True);
        }

        [Test]
        public async Task StopIQAsync_WhenNotConnected_DoesNothing()
        {
            await _client.StopIQAsync();

            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            _updMock.Verify(u => u.StopListening(), Times.Never);
        }

        [Test]
        public async Task StopIQAsync_WhenConnected_SendsMessageAndStopsUdp()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            await _client.StartIQAsync();

            await _client.StopIQAsync();

            _updMock.Verify(udp => udp.StopListening(), Times.Once);
            Assert.That(_client.IQStarted, Is.False);
        }

        [Test]
        public async Task ChangeFrequencyAsync_WhenNotConnected_DoesNothing()
        {
            await _client.ChangeFrequencyAsync(1000000, 1);

            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public async Task ChangeFrequencyAsync_WhenConnected_SendsMessage()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);

            await _client.ChangeFrequencyAsync(1000000, 1);

            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Once());
        }

        [Test]
        public void UnsolicitedMessageReceived_WhenNoRequestIsPending_RaisesEvent()
        {
            byte[]? receivedData = null;
            _client.UnsolicitedMessageReceived += (data) => receivedData = data;
            var messageFromServer = new byte[] { 1, 2, 3 };

            _tcpMock.Raise(m => m.MessageReceived += null, null, messageFromServer);

            Assert.That(receivedData, Is.Not.Null);
            Assert.That(receivedData, Is.EqualTo(messageFromServer));
        }

        [Test]
        public async Task SolicitedMessageReceived_CompletesPendingTask()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            var responseFromServer = new byte[] { 10, 20, 30 };

            var requestTask = _client.ChangeFrequencyAsync(12345, 1);

            _tcpMock.Raise(m => m.MessageReceived += null, null, responseFromServer);

            await requestTask;

            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        }

        [Test]
        public void SendTcpRequest_WhenServerDoesNotRespond_ThrowsTimeoutException()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            var messageToSend = new byte[] { 4, 5, 6 };

            Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await _client.StartIQAsync();
            });
        }
    }
}

