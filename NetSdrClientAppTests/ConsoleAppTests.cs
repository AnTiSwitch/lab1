using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Інтерфейси та клас-раннер для забезпечення тестування
public interface INetSdrClient
{
    Task ConnectAsync();
    void Disconect();
    Task ChangeFrequencyAsync(long freq, int receiverId);
    Task StartIQAsync();
    Task StopIQAsync();
    bool IQStarted { get; }
}

public interface IConsoleReader
{
    ConsoleKeyInfo ReadKey();
}

public class ConsoleAppRunner
{
    private readonly INetSdrClient _netSdr;
    private readonly IConsoleReader _consoleReader;

    public ConsoleAppRunner(INetSdrClient netSdr, IConsoleReader consoleReader)
    {
        _netSdr = netSdr;
        _consoleReader = consoleReader;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            var key = _consoleReader.ReadKey().Key;

            if (key == ConsoleKey.C)
            {
                await _netSdr.ConnectAsync();
            }
            else if (key == ConsoleKey.D)
            {
                _netSdr.Disconect();
            }
            else if (key == ConsoleKey.F)
            {
                await _netSdr.ChangeFrequencyAsync(20000000, 1);
            }
            else if (key == ConsoleKey.S)
            {
                if (_netSdr.IQStarted)
                {
                    await _netSdr.StopIQAsync();
                }
                else
                {
                    await _netSdr.StartIQAsync();
                }
            }
            else if (key == ConsoleKey.Q)
            {
                break;
            }
        }
    }
}


namespace NetSdrClientAppTests
{
    [TestFixture]
    public class ConsoleAppRunnerTests
    {
        private Mock<INetSdrClient> _mockNetSdr;
        private Mock<IConsoleReader> _mockConsoleReader;
        private ConsoleAppRunner _runner;

        [SetUp]
        public void Setup()
        {
            _mockNetSdr = new Mock<INetSdrClient>();
            _mockConsoleReader = new Mock<IConsoleReader>();

            _mockConsoleReader.Setup(r => r.ReadKey())
                              .Returns(() => new ConsoleKeyInfo(' ', ConsoleKey.Q, false, false, false));

            _runner = new ConsoleAppRunner(_mockNetSdr.Object, _mockConsoleReader.Object);
        }

        [Test]
        public async Task RunAsync_ExecutesAllCommandsCorrectly()
        {
            var keySequence = new Queue<ConsoleKey>(new[]
            {
                ConsoleKey.C,
                ConsoleKey.F,
                ConsoleKey.S,
                ConsoleKey.S,
                ConsoleKey.D,
                ConsoleKey.X,
                ConsoleKey.Q
            });

            _mockConsoleReader.Setup(r => r.ReadKey())
                              .Returns(() =>
                              {
                                  if (keySequence.Count == 0) return new ConsoleKeyInfo(' ', ConsoleKey.Q, false, false, false);
                                  var key = keySequence.Dequeue();
                                  return new ConsoleKeyInfo(' ', key, false, false, false);
                              });

            var iqStartedCounter = 0;
            _mockNetSdr.SetupGet(c => c.IQStarted)
                       .Returns(() =>
                       {
                           iqStartedCounter++;
                           return iqStartedCounter > 1;
                       });


            await _runner.RunAsync();

            _mockNetSdr.Verify(c => c.ConnectAsync(), Times.Once());
            _mockNetSdr.Verify(c => c.ChangeFrequencyAsync(20000000, 1), Times.Once());

            _mockNetSdr.VerifyGet(c => c.IQStarted, Times.Exactly(2));

            _mockNetSdr.Verify(c => c.StartIQAsync(), Times.Once());
            _mockNetSdr.Verify(c => c.StopIQAsync(), Times.Once());

            _mockNetSdr.Verify(c => c.Disconect(), Times.Once());

            _mockNetSdr.VerifyNoOtherCalls();
        }

        [Test]
        public async Task RunAsync_QuitsImmediatelyOnQ()
        {
            _mockConsoleReader.Setup(r => r.ReadKey())
                              .Returns(new ConsoleKeyInfo(' ', ConsoleKey.Q, false, false, false));

            await _runner.RunAsync();

            _mockNetSdr.Verify(c => c.ConnectAsync(), Times.Never());
            _mockNetSdr.Verify(c => c.Disconect(), Times.Never());
        }
    }
}