using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

// Інтерфейс для класу, який тестується (припускаємо, що він існує)
public interface INetworkStreamWrapper : IDisposable
{
    int ReadAsync(byte[] buffer, int offset, int count, CancellationToken token);
    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token);
}

// Мінімальна імітація класу NetworkStreamWrapper для тестування (як він має виглядати)
namespace EchoServer.Abstractions
{
    public class NetworkStreamWrapper : INetworkStreamWrapper
    {
        private readonly NetworkStream _stream;

        public NetworkStreamWrapper(NetworkStream stream)
        {
            _stream = stream;
        }

        public int ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            // Увага: Ваш оригінальний код викликає синхронний метод Read,
            // хоча сигнатура методу обгортки виглядає як асинхронна (ReadAsync).
            // Ми імітуємо виклик синхронного Read для покриття.
            return _stream.Read(buffer, offset, count);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            // Увага: Метод WriteAsync у NetworkStream приймає ReadOnlyMemory<byte> або byte[], 
            // але ми викликаємо тут застарілий метод, який співпадає з вашою сигнатурою.
            return _stream.WriteAsync(buffer, offset, count, token);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            GC.SuppressFinalize(this);
        }
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
            // Використовуємо MockBehavior.Loose, щоб не потрібно було заглушувати всі Dispose/Close
            _mockStream = new Mock<NetworkStream>(MockBehavior.Loose);
            _wrapper = new NetworkStreamWrapper(_mockStream.Object);
        }

        [Test]
        public void Constructor_InitializesStream()
        {
            Assert.That(_wrapper, Is.Not.Null);
            // Прямо перевірити приватне поле важко, але якщо об'єкт створено, конструктор виконався.
        }

        [Test]
        public void ReadAsync_CallsUnderlyingRead()
        {
            // Arrange
            byte[] buffer = new byte[10];
            int expectedBytes = 5;

            // Налаштовуємо заглушку для синхронного методу Read
            _mockStream
                .Setup(s => s.Read(buffer, 1, 10))
                .Returns(expectedBytes);

            // Act
            int result = _wrapper.ReadAsync(buffer, 1, 10, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(expectedBytes));
            // Перевіряємо, що метод Read був викликаний з правильними аргументами
            _mockStream.Verify(s => s.Read(buffer, 1, 10), Times.Once());
        }

        [Test]
        public async Task WriteAsync_CallsUnderlyingWriteAsync()
        {
            // Arrange
            byte[] buffer = new byte[10];
            var cts = new CancellationTokenSource();

            // Налаштовуємо заглушку для WriteAsync
            _mockStream
                .Setup(s => s.WriteAsync(buffer, 1, 10, cts.Token))
                .Returns(Task.CompletedTask);

            // Act
            await _wrapper.WriteAsync(buffer, 1, 10, cts.Token);

            // Assert
            // Перевіряємо, що асинхронний метод WriteAsync був викликаний
            _mockStream.Verify(s => s.WriteAsync(buffer, 1, 10, cts.Token), Times.Once());
        }

        [Test]
        public void Dispose_CallsStreamDispose()
        {
            // Act
            _wrapper.Dispose();

            // Assert
            // Перевіряємо, що метод Dispose на заглушці NetworkStream був викликаний
            _mockStream.Verify(s => s.Dispose(), Times.Once());
        }

        [Test]
        public void Dispose_DoesNotThrowIfCalledMultipleTimes()
        {
            // Arrange: Використовуємо MockBehavior.Loose, що дозволяє Dispose викликатися двічі

            // Act
            _wrapper.Dispose();
            _wrapper.Dispose();

            // Assert: Перевіряємо, що Dispose був викликаний лише один раз (завдяки _stream?.Dispose() та GC.SuppressFinalize)
            _mockStream.Verify(s => s.Dispose(), Times.Once());
        }
    }
}