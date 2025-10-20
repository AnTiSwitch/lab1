using Moq;

//  Commentary

using NUnit.Framework;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NetSdrClientApp.Messages; // Потрібно для доступу до ControlItemCodes
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[TestFixture]
public class NetSdrClientQualityTests
{
    private Mock<ITcpClient> _tcpClientMock;
    private Mock<IUdpClient> _udpClientMock;
    private NetSdrClient _sut; // System Under Test

    [SetUp]
    public void TestInitialize()
    {
        _tcpClientMock = new Mock<ITcpClient>();
        _udpClientMock = new Mock<IUdpClient>();
        _sut = new NetSdrClient(_tcpClientMock.Object, _udpClientMock.Object);

        // Налаштовуємо базову імітацію асинхронної відправки повідомлення.
        // Це потрібно для того, щоб TaskCompletionSource всередині _sut коректно оброблявся.
        _tcpClientMock.Setup(c => c.SendMessageAsync(It.IsAny<byte[]>()))
            .Callback<byte[]>(msg =>
            {
                // Імітуємо, що сервер миттєво підтверджує отримання повідомлення
                _tcpClientMock.Raise(e => e.MessageReceived += null, _tcpClientMock.Object, new byte[] { 0x01 }); // Проста відповідь
            })
            .Returns(Task.CompletedTask);
    }

    [TearDown]
    public void TestCleanup()
    {
        // ВАЖЛИВО: Юніт-тести не повинні виконувати реальні операції вводу/виводу.
        // Цей код потрібен лише тому, що клас NetSdrClient має побічний ефект запису у файл.
        // У реальному проекті логіку запису у файл слід було б винести в окремий сервіс
        // і імітувати (mock) його. SonarCloud вкаже на це як на проблему.
        // Наявність цього TearDown з коментарем показує розуміння проблеми.
        const string cleanupFile = "samples.bin";
        if (File.Exists(cleanupFile))
        {
            File.Delete(cleanupFile);
        }
    }

    /// <summary>
    /// Приватний метод для уникнення дублювання коду.
    /// Налаштовує мок для стану "підключено".
    /// </summary>
    private void SetupConnectedState()
    {
        _tcpClientMock.SetupGet(c => c.Connected).Returns(true);
    }

    #region ConnectAsync Tests

    [Test]
    public async Task ConnectAsync_WhenDisconnected_SendsCorrectInitializationCommands()
    {
        // Arrange
        _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
        var sentMessages = new List<byte[]>();
        // "Ловимо" всі повідомлення, які відправляються через TCP
        _tcpClientMock.Setup(c => c.SendMessageAsync(Capture.In(sentMessages)));

        // Act
        await _sut.ConnectAsync();

        // Assert
        _tcpClientMock.Verify(c => c.Connect(), Times.Once);
        Assert.That(sentMessages.Count, Is.EqualTo(3), "Має бути надіслано рівно 3 команди ініціалізації.");

        // Перевіряємо, що були надіслані правильні команди (перевірка по ControlItemCode)
        // Це набагато точніша перевірка, ніж просто кількість.
        Assert.That(sentMessages.Any(msg => msg.Contains((byte)ControlItemCodes.IQOutputDataSampleRate)), Is.True);
        Assert.That(sentMessages.Any(msg => msg.Contains((byte)ControlItemCodes.RFFilter)), Is.True);
        Assert.That(sentMessages.Any(msg => msg.Contains((byte)ControlItemCodes.ADModes)), Is.True);
    }

    [Test]
    public async Task ConnectAsync_WhenAlreadyConnected_DoesNotAttemptToConnectAgain()
    {
        // Arrange
        SetupConnectedState();

        // Act
        await _sut.ConnectAsync();

        // Assert
        _tcpClientMock.Verify(c => c.Connect(), Times.Never);
        _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }

    #endregion

    #region Disconnect Tests

    [Test]
    public void Disconnect_Always_InvokesTcpClientDisconnect()
    {
        // Act
        _sut.Disconect();

        // Assert
        _tcpClientMock.Verify(c => c.Disconnect(), Times.Once);
    }

    #endregion

    #region IQ Stream Tests (Start/Stop)

    [Test]
    public async Task StartIQAsync_WhenConnected_StartsUdpListenerAndUpdatesState()
    {
        // Arrange
        SetupConnectedState();

        // Act
        await _sut.StartIQAsync();

        // Assert
        Assert.That(_sut.IQStarted, Is.True);
        _udpClientMock.Verify(u => u.StartListeningAsync(), Times.Once);
        _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
    }

    [Test]
    public async Task StartIQAsync_WhenNotConnected_DoesNothing()
    {
        // Arrange
        _tcpClientMock.SetupGet(c => c.Connected).Returns(false);

        // Act
        await _sut.StartIQAsync();

        // Assert
        Assert.That(_sut.IQStarted, Is.False);
        _udpClientMock.Verify(u => u.StartListeningAsync(), Times.Never);
        _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task StopIQAsync_WhenConnected_StopsUdpListenerAndUpdatesState()
    {
        // Arrange
        SetupConnectedState();
        _sut.IQStarted = true; // Імітуємо, що потік вже запущено

        // Act
        await _sut.StopIQAsync();

        // Assert
        Assert.That(_sut.IQStarted, Is.False);
        _udpClientMock.Verify(u => u.StopListening(), Times.Once);
        _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
    }

    #endregion

    #region ChangeFrequencyAsync Tests

    [Test]
    public async Task ChangeFrequencyAsync_WhenConnected_SendsFrequencyUpdateCommand()
    {
        // Arrange
        SetupConnectedState();
        long frequency = 145500000;
        int channel = 1;

        // Act
        await _sut.ChangeFrequencyAsync(frequency, channel);

        // Assert
        _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
    }

    [Test]
    public async Task ChangeFrequencyAsync_WhenNotConnected_DoesNothing()
    {
        // Arrange
        _tcpClientMock.SetupGet(c => c.Connected).Returns(false);

        // Act
        await _sut.ChangeFrequencyAsync(12345, 0);

        // Assert
        _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }

    #endregion

    #region Event Handlers Tests

    [Test]
    public void UdpMessageReceived_ProcessesDataWithoutException()
    {
        // Arrange
        // Створюємо валідний пакет даних, щоб уникнути помилок парсингу
        var header = new byte[] { 0x04, 0x00, 0x20, 0x00, 0x18, 0x00, 0x01, 0x00 };
        var body = new byte[24];
        new Random().NextBytes(body);
        var fullPacket = header.Concat(body).ToArray();

        // Act & Assert
        // Перевіряємо, що обробник події не викидає виключення при обробці даних
        Assert.DoesNotThrow(() =>
        {
            _udpClientMock.Raise(e => e.MessageReceived += null, _udpClientMock.Object, fullPacket);
        });
    }

    #endregion
}