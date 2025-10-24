using System;
using NUnit.Framework; // <-- ������ � Xunit
					   // (�������, �����������: using NetSdrClientApp.Networking;)

namespace NetSdrClientAppTests
{
	public class UdpClientWrapperTests
	{
		[Test] // <-- ������ � [Fact]
		public void Exit_WhenNotStarted_DoesNotThrowException()
		{
			// Arrange
			var udpClient = new UdpClientWrapper(9000);

			// Act
			udpClient.StopListening();
			udpClient.Exit();

			// Assert
			// (���� �� ����� ����, ���� ��������)
			Assert.Pass();
		}
	}
}