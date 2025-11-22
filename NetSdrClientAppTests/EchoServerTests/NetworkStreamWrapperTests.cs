using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public interface INetworkStreamWrapper : IDisposable
{
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token);
    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token);
}

public class NetworkStreamWrapper : INetworkStreamWrapper
{
    private readonly NetworkStream _stream;

    public NetworkStreamWrapper(NetworkStream stream)
    {
        _stream = stream;
    }

    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
    {
        int bytesRead = _stream.Read(buffer, offset, count);
        return Task.FromResult(bytesRead);
    }

    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
    {
        return _stream.WriteAsync(buffer, offset, count, token);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}


namespace EchoServerTests
{
    [TestFixture]
    public class NetworkStreamWrapperTests
    {
        private Mock<NetworkStream> _mockStream;
        private NetworkStreamWrapper _wrapper;

        [SetUp]
        public void SetUp()
        {
            _mockStream = new Mock<NetworkStream>(MockBehavior.Loose);
            _wrapper = new NetworkStreamWrapper(_mockStream.Object);
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
                .Setup(s => s.Read(buffer, 1, 10))
                .Returns(expectedBytes);

            int result = await _wrapper.ReadAsync(buffer, 1, 10, CancellationToken.None);

            Assert.That(result, Is.EqualTo(expectedBytes));
            _mockStream.Verify(s => s.Read(buffer, 1, 10), Times.Once());
        }

        [Test]
        public async Task WriteAsync_CallsUnderlyingWriteAsync()
        {
            byte[] buffer = new byte[10];
            var cts = new CancellationTokenSource();

            _mockStream
                .Setup(s => s.WriteAsync(buffer, 1, 10, cts.Token))
                .Returns(Task.CompletedTask);

            await _wrapper.WriteAsync(buffer, 1, 10, cts.Token);

            _mockStream.Verify(s => s.WriteAsync(buffer, 1, 10, cts.Token), Times.Once());
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
            _wrapper.Dispose();
            _wrapper.Dispose();

            _mockStream.Verify(s => s.Dispose(), Times.Once());
        }
    }
}