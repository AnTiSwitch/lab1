using NUnit.Framework;
using EchoServer.Implementations;
using System.Net;
using EchoServer.Abstractions;
using static NUnit.Framework.Assert;

namespace NetSdrClientAppTests.EchoServerTests.ImplementationsTests
{
    [TestFixture]
    public class TcpListenerFactoryTests
    {
        [Test]
        public void Create_ReturnsTcpListenerWrapperInstance()
        {
            var factory = new TcpListenerFactory();
            ITcpListenerWrapper listener = factory.Create(IPAddress.Loopback, 5000);
            Assert.IsNotNull(listener);
            Assert.IsInstanceOf<TcpListenerWrapper>(listener);
        }
    }
}