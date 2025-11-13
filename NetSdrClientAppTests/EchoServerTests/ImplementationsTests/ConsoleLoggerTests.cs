using NUnit.Framework.Constraints;
using NUnit.Framework;
using EchoServer.Implementations;
using System;
using System.IO;
using static NUnit.Framework.Assert;

namespace NetSdrClientAppTests.EchoServerTests.ImplementationsTests
{
    [TestFixture]
    public class ConsoleLoggerTests
    {
        private ConsoleLogger _logger;
        private StringWriter _stringWriter;
        private TextWriter _originalConsoleOut;

        [SetUp]
        public void SetUp()
        {
            _logger = new ConsoleLogger();
            _stringWriter = new StringWriter();
            _originalConsoleOut = Console.Out;
            Console.SetOut(_stringWriter);
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalConsoleOut);
            _stringWriter.Dispose();
        }

        [Test]
        public void Log_WritesMessageWithPrefix()
        {
            const string message = "Server starting up.";
            _logger.Log(message);
            string output = _stringWriter.ToString();
            Assert.That(output, Does.Contain("[INFO]"));
            Assert.That(output, Does.Contain(message));
        }

        [Test]
        public void LogError_WritesErrorMessageWithPrefix()
        {
            const string message = "Network failed.";
            _logger.LogError(message);
            string output = _stringWriter.ToString();
            Assert.That(output, Does.Contain("[ERROR]"));
            Assert.That(output, Does.Contain(message));
        }
    }
}