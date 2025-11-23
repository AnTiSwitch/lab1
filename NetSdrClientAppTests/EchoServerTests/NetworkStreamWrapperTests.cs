using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions;

namespace EchoServerTests
{
    [TestFixture]
    public class NetworkStreamWrapperTests
    {
        private Mock<Stream> _mockStream;
        private NetworkStreamWrapper _wrapper;

        [SetUp]
        public void SetUp()
        {
            _mockStream = new Mock<Stream>(MockBehavior.Loose);
            _wrapper = new NetworkStreamWrapper(_mockStream.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _wrapper?.Dispose();
        }

        [Test]
        public void Constructor_InitializesStream()
        {
            Assert.That(_wrapper, Is.Not.Null);
        }

        [Test]
        public async Task ReadAsync_CallsUnderlyingRead()
        {
            byte[] buffer = new byte[10];
            int expectedBytes = 5;

            _mockStream
                .Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(expectedBytes);

            int result = await _wrapper.ReadAsync(buffer, 1, 10, CancellationToken.None);

            Assert.That(result, Is.EqualTo(expectedBytes));
            _mockStream.Verify(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once());
        }

        [Test]
        public async Task WriteAsync_CallsUnderlyingWriteAsync()
        {
            byte[] buffer = new byte[10];
            var cts = new CancellationTokenSource();

            _mockStream
                .Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _wrapper.WriteAsync(buffer, 1, 10, cts.Token);

            _mockStream.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public void Dispose_CallsStreamDispose()
        {
            _wrapper.Dispose();

            _mockStream.Verify(s => s.Dispose(), Times.Once());
        }

        [Test]
        public void Dispose_DoesNotThrowIfCalledMultipleTimes()
        {
            _mockStream.Setup(s => s.Dispose()).Verifiable();

            _wrapper.Dispose();
            _wrapper.Dispose();

            _mockStream.Verify(s => s.Dispose(), Times.Once());
        }
    }
}