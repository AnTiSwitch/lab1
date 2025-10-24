using System;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;
using NUnit.Framework; // <-- ������ � Xunit

namespace NetSdrClientAppTests
{
    public class TcpClientWrapperTests
    {
        [Test] // <-- ������ � [Fact]
        public void SendMessageAsync_Bytes_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var tcpClient = new TcpClientWrapper("127.0.0.1", 8080);
            var testData = new byte[] { 1, 2, 3 };

            // Act & Assert
            // (�� ��������� NUnit ��� �������� ����������� �������)
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await tcpClient.SendMessageAsync(testData));
        }

        [Test] // <-- ������ � [Fact]
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