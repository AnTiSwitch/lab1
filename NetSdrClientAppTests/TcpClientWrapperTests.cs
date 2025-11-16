using System;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    public class TcpClientWrapperTests
    {
        [Test] 
        public void SendMessageAsync_Bytes_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("localhost", _testPort);
            byte[] testData = new byte[] { 0x01, 0x02, 0x03 };

            // Act & Assert
            // (�� ��������� NUnit ��� �������� ����������� �������)
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await tcpClient.SendMessageAsync(testData));
        }

        [Test] 
        public void SendMessageAsync_String_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("localhost", _testPort);
            string testString = "Test";

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await tcpClient.SendMessageAsync(testString));
        }
    }
}