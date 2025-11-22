using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

public interface ILogger
{
    void Log(string message);
}

public interface IEchoServer
{
    Task StartAsync();
    void Stop();
}

public interface ITimedSender : IDisposable
{
    void StartSending(int intervalMilliseconds);
    void StopSending();
}

public interface IConsoleReader
{
    ConsoleKeyInfo ReadKey(bool intercept);
}

public class AppRunner
{
    private readonly IEchoServer _server;
    private readonly ILogger _logger;
    private readonly IConsoleReader _consoleReader;
    private readonly ITimedSender _sender;

    public AppRunner(IEchoServer server, ILogger logger, IConsoleReader consoleReader, ITimedSender sender)
    {
        _server = server;
        _logger = logger;
        _consoleReader = consoleReader;
        _sender = sender;
    }

    public async Task RunAppLogicAsync()
    {
        _logger.Log("Press any key to stop sending...");
        _sender.StartSending(5000);

        _logger.Log("Press 'q' to quit...");
        while (_consoleReader.ReadKey(intercept: true).Key != ConsoleKey.Q)
        {
        }

        _sender.StopSending();
        _server.Stop();
        _logger.Log("Sender stopped.");

        await Task.CompletedTask;
    }
}


namespace EchoServerTests
{
    [TestFixture]
    public class ProgramTests
    {
        private Mock<IEchoServer> _mockServer;
        private Mock<ILogger> _mockLogger;
        private Mock<IConsoleReader> _mockConsoleReader;
        private Mock<ITimedSender> _mockSender;
        private AppRunner _runner;

        [SetUp]
        public void Setup()
        {
            _mockServer = new Mock<IEchoServer>();
            _mockLogger = new Mock<ILogger>();
            _mockConsoleReader = new Mock<IConsoleReader>();
            _mockSender = new Mock<ITimedSender>();

            _mockConsoleReader.Setup(r => r.ReadKey(It.IsAny<bool>()))
                              .Returns(new ConsoleKeyInfo(' ', ConsoleKey.Q, false, false, false));

            _runner = new AppRunner(_mockServer.Object, _mockLogger.Object, _mockConsoleReader.Object, _mockSender.Object);
        }

        [Test]
        public async Task RunAppLogicAsync_ShouldExecuteAllStepsAndQuit()
        {
            var keySequence = new Queue<ConsoleKey>(new[]
            {
                ConsoleKey.A,
                ConsoleKey.Q
            });

            _mockConsoleReader.SetupSequence(r => r.ReadKey(It.IsAny<bool>()))
                              .Returns(new ConsoleKeyInfo(' ', keySequence.Dequeue(), false, false, false))
                              .Returns(new ConsoleKeyInfo(' ', keySequence.Dequeue(), false, false, false));


            await _runner.RunAppLogicAsync();

            _mockLogger.Verify(l => l.Log("Press any key to stop sending..."), Times.Once());
            _mockLogger.Verify(l => l.Log("Press 'q' to quit..."), Times.Once());
            _mockLogger.Verify(l => l.Log("Sender stopped."), Times.Once());

            _mockSender.Verify(s => s.StartSending(5000), Times.Once());
            _mockSender.Verify(s => s.StopSending(), Times.Once());

            _mockServer.Verify(s => s.Stop(), Times.Once());
        }

        [Test]
        public async Task RunAppLogicAsync_ShouldQuitImmediately()
        {
            _mockConsoleReader.Setup(r => r.ReadKey(It.IsAny<bool>()))
                              .Returns(new ConsoleKeyInfo(' ', ConsoleKey.Q, false, false, false));

            await _runner.RunAppLogicAsync();

            _mockSender.Verify(s => s.StartSending(It.IsAny<int>()), Times.Once());
            _mockSender.Verify(s => s.StopSending(), Times.Once());
            _mockServer.Verify(s => s.Stop(), Times.Once());

            _mockConsoleReader.Verify(r => r.ReadKey(It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public async Task RunAppLogicAsync_ShouldHandleMultipleKeysBeforeQuit()
        {
            _mockConsoleReader.SetupSequence(r => r.ReadKey(It.IsAny<bool>()))
                              .Returns(new ConsoleKeyInfo(' ', ConsoleKey.D, false, false, false))
                              .Returns(new ConsoleKeyInfo(' ', ConsoleKey.F, false, false, false))
                              .Returns(new ConsoleKeyInfo(' ', ConsoleKey.Q, false, false, false));

            await _runner.RunAppLogicAsync();

            _mockConsoleReader.Verify(r => r.ReadKey(It.IsAny<bool>()), Times.Exactly(3));
        }
    }
}