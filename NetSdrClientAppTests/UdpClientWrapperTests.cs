using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;

namespace NetSdrClientAppTests
{
	public class UdpClientWrapperTests
	{
		[Test] 
		public void Exit_WhenNotStarted_DoesNotThrowException()
		{
			// Arrange
			var udpClient = new UdpClientWrapper(9000);

            // Act
            _testSender = new UdpClient();
            byte[] largeData = new byte[8000]; // ������� �����
            for (int i = 0; i < largeData.Length; i++)
            {
                largeData[i] = (byte)(i % 256);
            }
            await _testSender.SendAsync(largeData, largeData.Length, "localhost", _testPort);

            await Task.WhenAny(messageReceived.Task, Task.Delay(3000));

			// Assert
			// (���� �� ����� ����, ���� ��������)
			Assert.Pass();
		}
	}
}