using System;
using NUnit.Framework; // <-- «м≥нено з Xunit
					   // (ћожливо, знадобитьс€: using NetSdrClientApp.Networking;)

namespace NetSdrClientAppTests
{
	public class UdpClientWrapperTests
	{
		[Test] // <-- «м≥нено з [Fact]
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