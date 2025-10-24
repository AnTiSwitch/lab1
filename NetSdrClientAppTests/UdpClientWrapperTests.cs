using System;
using Xunit;
// Можливо, знадобиться: using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests
{
	public class UdpClientWrapperTests
	{
		[Fact]
		public void Exit_WhenNotStarted_DoesNotThrowException()
		{
			// Arrange (Налаштування)
			var udpClient = new UdpClientWrapper(9000);

			// Act (Дія)
			// Ми просто викликаємо методи, які ти виправив
			// Якщо код "впаде", тест провалиться
			udpClient.StopListening();
			udpClient.Exit();

			// Assert (Перевірка)
			// Якщо ми дійшли сюди без помилок, тест пройдено
			Assert.True(true);
		}
	}
}