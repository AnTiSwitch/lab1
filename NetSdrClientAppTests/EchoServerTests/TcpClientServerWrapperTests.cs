using System;
using System.Net.Sockets;
using Moq;
using Xunit;
using EchoServer.Abstractions;
using Assert = Xunit.Assert;

public class TcpClientServerWrapperTests
{
    [Fact]
    public void Constructor_InitializesClient()
    {
        var mockTcpClient = new Mock<TcpClient>();

        var wrapper = new TcpClientWrapper(mockTcpClient.Object);

        Assert.NotNull(wrapper);
    }

    [Fact]
    public void GetStream_CallsGetStreamAndReturnsWrapper()
    {
        var mockNetworkStream = new Mock<NetworkStream>(MockBehavior.Strict);
        var mockTcpClient = new Mock<TcpClient>(MockBehavior.Strict);

        mockTcpClient.Setup(c => c.GetStream()).Returns(mockNetworkStream.Object).Verifiable();

        var wrapper = new TcpClientWrapper(mockTcpClient.Object);

        var streamWrapper = wrapper.GetStream();

        mockTcpClient.Verify(c => c.GetStream(), Times.Once);

        Assert.NotNull(streamWrapper);
        Assert.IsType<NetworkStreamWrapper>(streamWrapper);
        Assert.IsAssignableFrom<INetworkStreamWrapper>(streamWrapper);
    }

    [Fact]
    public void Close_CallsClientClose()
    {
        var mockTcpClient = new Mock<TcpClient>(MockBehavior.Strict);

        mockTcpClient.Setup(c => c.Close()).Verifiable();

        var wrapper = new TcpClientWrapper(mockTcpClient.Object);

        wrapper.Close();

        mockTcpClient.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public void Dispose_CallsClientDisposeAndSuppressesFinalize()
    {
        var mockTcpClient = new Mock<TcpClient>(MockBehavior.Strict);

        mockTcpClient.Setup(c => c.Dispose()).Verifiable();

        var wrapper = new TcpClientWrapper(mockTcpClient.Object);

        wrapper.Dispose();

        mockTcpClient.Verify(c => c.Dispose(), Times.Once);
    }
}