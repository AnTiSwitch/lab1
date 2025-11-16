using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoServer.Abstractions;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

public class NetworkStreamWrapperTests
{
    [Fact]
    public async Task ReadAsync_ShouldCallUnderlyingStreamReadAsync()
    {
        // Arrange
        var buffer = new byte[10];
        var token = CancellationToken.None;

        int expectedBytes = 7;
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        mockStream
            .Setup(s => s.ReadAsync(buffer, 0, 10, token))
            .ReturnsAsync(expectedBytes);

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act
        int actual = await wrapper.ReadAsync(buffer, 0, 10, token);

        // Assert
        Assert.Equal(expectedBytes, actual);
        mockStream.Verify(s => s.ReadAsync(buffer, 0, 10, token), Times.Once);
    }

    [Fact]
    public async Task ReadAsync_ShouldThrowIfStreamThrows()
    {
        // Arrange
        var buffer = new byte[10];
        var token = CancellationToken.None;

        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        mockStream
            .Setup(s => s.ReadAsync(buffer, 0, 10, token))
            .ThrowsAsync(new InvalidOperationException());

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapper.ReadAsync(buffer, 0, 10, token));
    }

    [Fact]
    public async Task WriteAsync_ShouldCallUnderlyingStreamWriteAsync()
    {
        // Arrange
        var buffer = new byte[10];
        var token = CancellationToken.None;

        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        mockStream
            .Setup(s => s.WriteAsync(buffer, 0, 10, token))
            .Returns(Task.CompletedTask);

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act
        await wrapper.WriteAsync(buffer, 0, 10, token);

        // Assert
        mockStream.Verify(s => s.WriteAsync(buffer, 0, 10, token), Times.Once);
    }

    [Fact]
    public async Task WriteAsync_ShouldThrowIfStreamThrows()
    {
        // Arrange
        var buffer = new byte[10];
        var token = CancellationToken.None;

        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        mockStream
            .Setup(s => s.WriteAsync(buffer, 0, 10, token))
            .ThrowsAsync(new InvalidOperationException());

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapper.WriteAsync(buffer, 0, 10, token));
    }

    [Fact]
    public void Dispose_ShouldCallDisposeOnStream()
    {
        // Arrange
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        mockStream.Setup(s => s.Dispose());

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act
        wrapper.Dispose();

        // Assert
        mockStream.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldNotThrowIfCalledTwice()
    {
        // Arrange
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        mockStream.Setup(s => s.Dispose());

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act
        wrapper.Dispose();
        wrapper.Dispose(); // second call must not throw

        // Assert
        mockStream.Verify(s => s.Dispose(), Times.Once);
    }
}
