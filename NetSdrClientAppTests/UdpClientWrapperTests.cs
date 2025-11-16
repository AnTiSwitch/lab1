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
            udpClient.StopListening();
            udpClient.Exit();

            // Assert
            Assert.Pass();
        }
    }
}