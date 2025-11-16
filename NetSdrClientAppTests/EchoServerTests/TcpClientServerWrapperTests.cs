using System;
using System.Net.Sockets;
using EchoServer.Abstractions;
using Moq;
using Xunit;
using Assert = Xunit.Assert;


public class TcpClientServerWrapperTests
{
    [Fact]
    public void Constructor_ShouldStoreClient()
    {
        // Arrange
        var client = new TcpClient();

        // Act
        var wrapper = new TcpClientWrapper(client);

        // Assert
        Assert.NotNull(wrapper);
    }

    [Fact]
    public void GetStream_ShouldReturnNetworkStreamWrapper()
    {
        // Arrange
        var mockClient = new Mock<TcpClient>(MockBehavior.Strict);
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);

        mockClient
            .Setup(c => c.GetStream())
            .Returns(mockStream.Object);

        var wrapper = new TcpClientWrapper(mockClient.Object);

        // Act
        var result = wrapper.GetStream();

        // Assert
        Assert.IsAssignableFrom<INetworkStreamWrapper>(result);
        Assert.IsType<NetworkStreamWrapper>(result);
        mockClient.Verify(c => c.GetStream(), Times.Once);
    }

    [Fact]
    public void Close_ShouldCallClientClose()
    {
        // Arrange
        var mockClient = new Mock<TcpClient>(MockBehavior.Strict);
        mockClient.Setup(c => c.Close());

        var wrapper = new TcpClientWrapper(mockClient.Object);

        // Act
        wrapper.Close();

        // Assert
        mockClient.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldCallClientDispose()
    {
        // Arrange
        var mockClient = new Mock<TcpClient>(MockBehavior.Strict);
        mockClient.Setup(c => c.Dispose());

        var wrapper = new TcpClientWrapper(mockClient.Object);

        // Act
        wrapper.Dispose();

        // Assert
        mockClient.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldNotThrowIfCalledTwice()
    {
        // Arrange
        var mockClient = new Mock<TcpClient>(MockBehavior.Strict);
        mockClient.Setup(c => c.Dispose());

        var wrapper = new TcpClientWrapper(mockClient.Object);

        // Act
        wrapper.Dispose();
        wrapper.Dispose(); // second call must NOT throw

        // Assert
        mockClient.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public void GetStream_ShouldThrowIfClientThrows()
    {
        // Arrange
        var mockClient = new Mock<TcpClient>(MockBehavior.Strict);
        mockClient
            .Setup(c => c.GetStream())
            .Throws(new InvalidOperationException());

        var wrapper = new TcpClientWrapper(mockClient.Object);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => wrapper.GetStream());
    }
}
