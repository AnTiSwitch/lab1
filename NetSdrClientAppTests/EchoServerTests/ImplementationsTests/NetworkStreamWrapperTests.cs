using NUnit.Framework;
using EchoServer.Implementations;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Net;
using static NUnit.Framework.Assert;

namespace NetSdrClientAppTests.EchoServerTests.ImplementationsTests
{
    [TestFixture]
    public class NetworkStreamWrapperTests
    {
        // Допоміжний клас для імітації NetworkStream
        private class MockNetworkStream : NetworkStream
        {
            private readonly Stream _baseStream;
            public MockNetworkStream(Stream baseStream) : base(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), true)
            {
                _baseStream = baseStream;
            }
            // Перевизначаємо ключові методи для використання MemoryStream
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        [Test]
        public async Task ReadAsync_DelegatesToInternalStream()
        {
            var testData = new byte[] { 1, 2, 3 };
            using (var memoryStream = new MemoryStream(testData))
            {
                var wrapper = new NetworkStreamWrapper(new MockNetworkStream(memoryStream));
                var buffer = new byte[5];
                int bytesRead = await wrapper.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

                Assert.AreEqual(3, bytesRead);
                Assert.AreEqual(1, buffer[0]);
            }
        }

        [Test]
        public async Task WriteAsync_DelegatesToInternalStream()
        {
            var testData = new byte[] { 4, 5 };
            using (var memoryStream = new MemoryStream())
            {
                var wrapper = new NetworkStreamWrapper(new MockNetworkStream(memoryStream));

                await wrapper.WriteAsync(testData, 0, testData.Length, CancellationToken.None);

                Assert.AreEqual(2, memoryStream.Length);
                memoryStream.Position = 0;
                Assert.AreEqual(4, memoryStream.ReadByte());
            }
        }
    }
}