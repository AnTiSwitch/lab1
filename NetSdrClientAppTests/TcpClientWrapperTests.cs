using System;
using System.Threading.Tasks;
using NetSdrClientApp.Networking; // Переконайся, що цей 'using' правильний
using Xunit;

namespace NetSdrClientAppTests
{
    public class TcpClientWrapperTests
    {
        [Fact]
        public async Task SendMessageAsync_Bytes_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange (Налаштування)
            // Створюємо wrapper, але НЕ викликаємо .Connect()
            var tcpClient = new TcpClientWrapper("127.0.0.1", 8080);
            var testData = new byte[] { 1, 2, 3 };

            // Act & Assert (Дія та Перевірка)
            // Ми очікуємо, що цей виклик "впаде" з помилкою InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(() => tcpClient.SendMessageAsync(testData));
        }

        [Fact]
        public async Task SendMessageAsync_String_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange (Налаштування)
            var tcpClient = new TcpClientWrapper("127.0.0.1", 8080);
            var testString = "hello";

            // Act & Assert (Дія та Перевірка)
            // Ми також очікуємо, що цей виклик "впаде" з тією ж помилкою
            // Це доведе, що наш рефакторинг (виклик одного методу з іншого) працює
            await Assert.ThrowsAsync<InvalidOperationException>(() => tcpClient.SendMessageAsync(testString));
        }
    }
}