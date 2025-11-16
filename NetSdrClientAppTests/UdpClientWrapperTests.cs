using System;
using NUnit.Framework;
using NetSdrClientApp.Networking;

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
			udpClient.StopListening();
			udpClient.Exit();

			// Assert
			// (якщо ми д≥йшли сюди, тест пройдено)
			Assert.Pass();
		}
	}
}
