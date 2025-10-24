using System;
using Xunit;
// �������, �����������: using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests
{
	public class UdpClientWrapperTests
	{
		[Fact]
		public void Exit_WhenNotStarted_DoesNotThrowException()
		{
			// Arrange (������������)
			var udpClient = new UdpClientWrapper(9000);

			// Act (ĳ�)
			// �� ������ ��������� ������, �� �� ��������
			// ���� ��� "�����", ���� �����������
			udpClient.StopListening();
			udpClient.Exit();

			// Assert (��������)
			// ���� �� ����� ���� ��� �������, ���� ��������
			Assert.True(true);
		}
	}
}