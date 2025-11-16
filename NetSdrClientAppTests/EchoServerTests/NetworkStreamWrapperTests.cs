
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using EchoServer.Abstractions;

public class NetworkStreamWrapperTests
{
    private readonly CancellationToken _defaultToken = CancellationToken.None;

    [Fact]
    public void Constructor_InitializesStream()
    {
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        Assert.NotNull(wrapper);
    }

    [Fact]
    public async Task ReadAsync_CallsUnderlyingReadAsync()
    {
        // Arrange
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        var buffer = new byte[10];
        var expectedBytesRead = 5;

        // Налаштування: Очікуємо виклику ReadAsync і повертаємо очікуваний результат
        mockStream
            .Setup(s => s.ReadAsync(buffer, 0, 10, _defaultToken))
            .ReturnsAsync(expectedBytesRead)
            .Verifiable();

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act
        var bytesRead = await wrapper.ReadAsync(buffer, 0, 10, _defaultToken);

        // Assert
        Assert.Equal(expectedBytesRead, bytesRead);
        mockStream.Verify(s => s.ReadAsync(buffer, 0, 10, _defaultToken), Times.Once);
    }

    [Fact]
    public async Task WriteAsync_CallsUnderlyingWriteAsync()
    {
        // Arrange
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);
        var buffer = new byte[10];

        // Налаштування: Очікуємо виклику WriteAsync
        mockStream
            .Setup(s => s.WriteAsync(buffer, 0, 10, _defaultToken))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act
        await wrapper.WriteAsync(buffer, 0, 10, _defaultToken);

        // Assert
        mockStream.Verify(s => s.WriteAsync(buffer, 0, 10, _defaultToken), Times.Once);
    }

    [Fact]
    public void Dispose_CallsUnderlyingDisposeAndSuppressesFinalize()
    {
        // Arrange
        var mockStream = new Mock<NetworkStream>(MockBehavior.Strict);

        // Налаштування: Очікуємо виклику Dispose
        mockStream.Setup(s => s.Dispose()).Verifiable();

        var wrapper = new NetworkStreamWrapper(mockStream.Object);

        // Act
        wrapper.Dispose();

        // Assert
        mockStream.Verify(s => s.Dispose(), Times.Once);
    }
}