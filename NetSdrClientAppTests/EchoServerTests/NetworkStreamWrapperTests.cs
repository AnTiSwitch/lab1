using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions; // ВИКОРИСТОВУЄМО РЕАЛЬНИЙ ПРОСТІР ІМЕН

// NetworkStreamWrapper більше не визначається тут, він повинен бути доступний через EchoServer.dll
// (або в тестовому проєкті є посилання на реальний клас)

namespace EchoServerTests
{
    // Щоб клас NetworkStreamWrapper був доступний для створення екземпляра
    // (якщо він визначений у EchoServer.dll), ми використовуємо його.
    // Якщо він невидимий, це може бути проблемою архітектури, але для тестування
    // ми повинні припустити, що клас доступний для створення в цьому проєкті.

    [TestFixture]
    public class NetworkStreamWrapperTests
    {
        private Mock<NetworkStream> _mockStream;
        private NetworkStreamWrapper _wrapper;

        [SetUp]
        public void SetUp()
        {
            _mockStream = new Mock<NetworkStream>(MockBehavior.Loose);
            // Якщо NetworkStreamWrapper не є частиною EchoServer.dll,
            // а створений в іншому місці, то це може бути помилкою.
            // Припускаємо, що він доступний або має бути створений
            // Тимчасово припускаємо, що клас доступний
            _wrapper = new NetworkStreamWrapper(_mockStream.Object);
        }

        [TearDown]
        public void TearDown()
        {
            // ВИПРАВЛЕННЯ NUnit1032
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

            // Налаштовуємо заглушку для синхронного методу Read
            _mockStream
                .Setup(s => s.Read(buffer, 1, 10))
                .Returns(expectedBytes);

            // ВИПРАВЛЕНО: Ми більше не визначаємо INetworkStreamWrapper тут.
            // Клас NetworkStreamWrapper повинен бути доступний для створення
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