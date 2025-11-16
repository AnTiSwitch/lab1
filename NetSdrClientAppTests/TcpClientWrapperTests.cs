using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NetSdrClientApp.Networking;
using NUnit.Framework;

namespace NetSdrClientApp.Tests.Networking
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        [Test] 
        public void SendMessageAsync_Bytes_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("localhost", _testPort);
            byte[] testData = new byte[] { 0x01, 0x02, 0x03 };

            // Act & Assert
            // (Це синтаксис NUnit для перевірки асинхронних винятків)
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await tcpClient.SendMessageAsync(testData));
        }

        [Test] 
        public void SendMessageAsync_String_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("localhost", _testPort);
            string testString = "Test";

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await wrapper.SendMessageAsync(testString)
            );
        }

        [Test]
        public async Task MessageReceived_Event_RaisedWhenDataReceived()
        {
            // Arrange
            Assert.That(_testServer, Is.Not.Null);
            _testServer.Start();
            var wrapper = new TcpClientWrapper("localhost", _testPort);

            byte[]? receivedMessage = null;
            var messageReceivedEvent = new TaskCompletionSource<bool>();

            wrapper.MessageReceived += (sender, data) =>
            {
                receivedMessage = data;
                messageReceivedEvent.TrySetResult(true);
            };

            var serverTask = Task.Run(async () =>
            {
                var client = await _testServer.AcceptTcpClientAsync();
                await Task.Delay(200); // Даємо час клієнту підключитись
                var stream = client.GetStream();
                byte[] testData = new byte[] { 0xAA, 0xBB, 0xCC };
                await stream.WriteAsync(testData.AsMemory(0, testData.Length));
                await Task.Delay(100);
                client.Close();
            });

            // Act
            wrapper.Connect();
            await Task.WhenAny(messageReceivedEvent.Task, Task.Delay(2000));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(receivedMessage, Is.Not.Null);
                Assert.That(receivedMessage, Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC }));
            });

            // Cleanup
            wrapper.Disconnect();
            await serverTask;
        }

        [Test]
        public async Task MultipleMessages_AreReceivedCorrectly()
        {
            // Arrange
            Assert.That(_testServer, Is.Not.Null);
            _testServer.Start();
            var wrapper = new TcpClientWrapper("localhost", _testPort);

            var messagesReceived = new System.Collections.Concurrent.ConcurrentBag<byte[]>();
            var messageCount = new TaskCompletionSource<bool>();
            int expectedMessages = 3;

            wrapper.MessageReceived += (sender, data) =>
            {
                messagesReceived.Add(data);
                if (messagesReceived.Count >= expectedMessages)
                {
                    messageCount.TrySetResult(true);
                }
            };

            var serverTask = Task.Run(async () =>
            {
                var client = await _testServer.AcceptTcpClientAsync();
                await Task.Delay(200);
                var stream = client.GetStream();

                for (int i = 1; i <= expectedMessages; i++)
                {
                    byte[] testData = new byte[] { (byte)i };
                    await stream.WriteAsync(testData.AsMemory(0, testData.Length));
                    await Task.Delay(50);
                }

                await Task.Delay(100);
                client.Close();
            });

            // Act
            wrapper.Connect();
            await Task.WhenAny(messageCount.Task, Task.Delay(3000));

            // Assert
            Assert.That(messagesReceived, Has.Count.EqualTo(expectedMessages));

            // Cleanup
            wrapper.Disconnect();
            await serverTask;
        }

        [Test]
        public async Task Disconnect_StopsListening()
        {
            // Arrange
            Assert.That(_testServer, Is.Not.Null);
            _testServer.Start();
            var wrapper = new TcpClientWrapper("localhost", _testPort);
            bool messageReceived = false;

            wrapper.MessageReceived += (sender, data) =>
            {
                messageReceived = true;
            };

            var serverTask = Task.Run(async () =>
            {
                var client = await _testServer.AcceptTcpClientAsync();
                await Task.Delay(200);
                return client;
            });

            wrapper.Connect();
            await Task.Delay(100);
            var serverClient = await serverTask;

            // Act
            wrapper.Disconnect();
            await Task.Delay(100);

            // Спробуємо відправити дані після відключення
            try
            {
                var stream = serverClient.GetStream();
                byte[] data = new byte[] { 0x01 };
                await stream.WriteAsync(data.AsMemory(0, data.Length));
            }
            catch { }

            await Task.Delay(200);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(messageReceived, Is.False); // Не повинні отримати повідомлення після відключення
                Assert.That(wrapper.Connected, Is.False);
            });

            // Cleanup
            serverClient?.Close();
        }

        // Helper methods
        private static async Task AcceptClientAsync(TcpListener server, CancellationToken ct)
        {
            try
            {
                var client = await server.AcceptTcpClientAsync(ct);
                await Task.Delay(500, ct);
                client.Close();
            }
            catch (OperationCanceledException) { }
        }

        private static async Task AcceptAndReceiveAsync(TcpListener server, TaskCompletionSource<byte[]> receivedData, CancellationToken ct)
        {
            try
            {
                var client = await server.AcceptTcpClientAsync(ct);
                var stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
                receivedData.SetResult(buffer.Take(bytesRead).ToArray());
                await Task.Delay(100, ct);
                client.Close();
            }
            catch (OperationCanceledException) { }
        }
    }
}