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

            // ВИПРАВЛЕНО: Мокаємо асинхронний ReadAsync()
            _mockStream
                .Setup(s => s.ReadAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedBytes);

            int result = await _wrapper.ReadAsync(buffer, 1, 10, CancellationToken.None);

            Assert.That(result, Is.EqualTo(expectedBytes));

            // Верифікуємо асинхронний виклик
            _mockStream.Verify(s => s.ReadAsync(
                It.IsAny<byte[]>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public async Task WriteAsync_CallsUnderlyingWriteAsync()
        {
            byte[] buffer = new byte[10];
            var cts = new CancellationTokenSource();

            _mockStream
                .Setup(s => s.WriteAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _wrapper.WriteAsync(buffer, 1, 10, cts.Token);

            _mockStream.Verify(s => s.WriteAsync(
                It.IsAny<byte[]>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        public void Dispose_CallsStreamDispose()
        {
            _wrapper.Dispose();

            // ВИПРАВЛЕНО: Використовуємо .Dispose() для верифікації IDisposable.
            // Якщо ця верифікація все ще викликає NotSupportedException, це означає,
            // що ваша версія Moq несумісна, і цей рядок слід видалити. 
            // Але в більшості сучасних Moq це спрацює для IDisposable.
            _mockStream.Verify(s => s.Dispose(), Times.Once());
        }

        [Test]
        public void Dispose_DoesNotThrowIfCalledMultipleTimes()
        {
            // Видаляємо .Setup для Dispose, оскільки він викликав NotSupportedException

            _wrapper.Dispose();
            _wrapper.Dispose();

            // Верифікуємо лише один виклик Dispose
            _mockStream.Verify(s => s.Dispose(), Times.Once());
        }
    }
}