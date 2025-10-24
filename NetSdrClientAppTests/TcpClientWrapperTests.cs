using System;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;
using NUnit.Framework; // <-- «м≥нено з Xunit

namespace NetSdrClientAppTests
{
    public class TcpClientWrapperTests
    {
        [Test] // <-- «м≥нено з [Fact]
        public void SendMessageAsync_Bytes_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var tcpClient = new TcpClientWrapper("127.0.0.1", 8080);
            var testData = new byte[] { 1, 2, 3 };

            // Act & Assert
            // (÷е синтаксис NUnit дл€ перев≥рки асинхронних вин€тк≥в)
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await tcpClient.SendMessageAsync(testData));
        }

        [Test] // <-- «м≥нено з [Fact]
        public void SendMessageAsync_String_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var tcpClient = new TcpClientWrapper("127.0.0.1", 8080);
            var testString = "hello";

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await tcpClient.SendMessageAsync(testString));
        }
    }
}