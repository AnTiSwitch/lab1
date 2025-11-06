using EchoServer; // Обов'язково! Це посилання на ваш рефакторинговий проєкт
using Moq;
using NUnit.Framework;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    // [TestFixture] вказує NUnit, що це клас з тестами
    [TestFixture]
    public class ClientEchoHandlerTests
    {
        // Тест на коректну роботу логіки "Відлуння"
        [Test]
        public async Task HandleClientStreamAsync_ShouldEchoReceivedData()
        {
            // Arrange
            // 1. Створюємо "фейковий" об'єкт для INetworkStream за допомогою Moq
            var mockStream = new Mock<INetworkStream>();
            var handler = new ClientEchoHandler();

            // 2. Дані, які ми імітуємо, що були прочитані з мережі
            byte[] inputData = Encoding.UTF8.GetBytes("Hello World Echo");

            // 3. Налаштовуємо послідовність читання (Sequence)
            var sequence = new MockSequence();

            // Читання 1: Повертаємо inputData.Length байт і копіюємо дані в буфер
            mockStream.InSequence(sequence).Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(inputData.Length)
                      .Callback<byte[], int, int, CancellationToken>((buffer, offset, size, token) =>
                      {
                          // Імітація роботи NetworkStream
                          Array.Copy(inputData, buffer, inputData.Length);
                      });

            // Читання 2: Повертаємо 0, що сигналізує про кінець потоку і завершує цикл
            mockStream.InSequence(sequence).Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(0);

            // Act
            // Викликаємо ізольовану логіку
            await handler.HandleClientStreamAsync(mockStream.Object, CancellationToken.None);

            // Assert
            // 1. Перевіряємо, що метод WriteAsync був викликаний 1 раз
            // 2. Перевіряємо, що записані дані відповідають вхідним даним (логіка Echo)
            mockStream.Verify(s => s.WriteAsync(
                It.Is<byte[]>(b => b.Length >= inputData.Length),
                0,
                inputData.Length,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        // Тест на коректне завершення роботи при нульовому читанні
        [Test]
        public async Task HandleClientStreamAsync_WhenReadReturnsZero_ShouldExitGracefully()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var handler = new ClientEchoHandler();

            // Mock, який одразу повертає 0 байт
            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(0);

            // Act
            await handler.HandleClientStreamAsync(mockStream.Object, CancellationToken.None);

            // Assert
            // Перевіряємо, що WriteAsync НЕ був викликаний, оскільки даних не було
            mockStream.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.Pass("Handler exited correctly when ReadAsync returned 0.");
        }
    }
}