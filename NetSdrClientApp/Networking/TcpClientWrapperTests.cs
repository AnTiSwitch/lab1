using System;
using System.Threading.Tasks;
using NetSdrClientApp.Networking; // �����������, �� ��� 'using' ����������
using Xunit;

namespace NetSdrClientAppTests
{
    public class TcpClientWrapperTests
    {
        [Fact]
        public async Task SendMessageAsync_Bytes_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange (������������)
            // ��������� wrapper, ��� �� ��������� .Connect()
            var tcpClient = new TcpClientWrapper("127.0.0.1", 8080);
            var testData = new byte[] { 1, 2, 3 };

            // Act & Assert (ĳ� �� ��������)
            // �� �������, �� ��� ������ "�����" � �������� InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(() => tcpClient.SendMessageAsync(testData));
        }

        [Fact]
        public async Task SendMessageAsync_String_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange (������������)
            var tcpClient = new TcpClientWrapper("127.0.0.1", 8080);
            var testString = "hello";

            // Act & Assert (ĳ� �� ��������)
            // �� ����� �������, �� ��� ������ "�����" � �� � ��������
            // �� ������, �� ��� ����������� (������ ������ ������ � ������) ������
            await Assert.ThrowsAsync<InvalidOperationException>(() => tcpClient.SendMessageAsync(testString));
        }
    }
}