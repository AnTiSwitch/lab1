using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

namespace NetSdrClientApp
{
    public class NetSdrClient
    {
        // ПОМИЛКА 1 (ВИПРАВЛЕНО): 'readonly' має стояти після 'private'.
        private readonly ITcpClient _tcpClient;
        private readonly IUdpClient _udpClient;

        public bool IQStarted { get; private set; }

        // ПОМИЛКА 5 (ДОДАНО): Оголошення події для незапрошених повідомлень.
        public event Action<byte[]>? UnsolicitedMessageReceived;

        // ПОМИЛКА 2 (ВИПРАВЛЕНО): Конструктор не може бути 'static'.
        public NetSdrClient(ITcpClient tcpClient, IUdpClient udpClient)
        {
            _tcpClient = tcpClient;
            _udpClient = udpClient;

            _tcpClient.MessageReceived += _tcpClient_MessageReceived;
            _udpClient.MessageReceived += _udpClient_MessageReceived;
        }

        public async Task ConnectAsync()
        {
            if (!_tcpClient.Connected)
            {
                _tcpClient.Connect();

                var sampleRate = BitConverter.GetBytes((long)100000).Take(5).ToArray();
                var automaticFilterMode = BitConverter.GetBytes((ushort)0).ToArray();
                var adMode = new byte[] { 0x00, 0x03 };

                //Host pre setup
                var msgs = new List<byte[]>
                {
                    NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.IQOutputDataSampleRate, sampleRate),
                    NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.RFFilter, automaticFilterMode),
                    NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ADModes, adMode),
                };

                foreach (var msg in msgs)
                {
                    await SendTcpRequest(msg);
                }
            }
        }

        public void Disconnect()
        {
            _tcpClient.Disconnect();
        }

        public async Task StartIQAsync()
        {
            if (!_tcpClient.Connected)
            {
                Console.WriteLine("No active connection.");
                return;
            }

            var iqDataMode = (byte)0x80;
            var start = (byte)0x02;
            var fifo16bitCaptureMode = (byte)0x01;
            var n = (byte)1;

            var args = new[] { iqDataMode, start, fifo16bitCaptureMode, n };

            var msg = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, args);

            await SendTcpRequest(msg);

            IQStarted = true;

            _ = _udpClient.StartListeningAsync();
        }

        public async Task StopIQAsync()
        {
            if (!_tcpClient.Connected)
            {
                Console.WriteLine("No active connection.");
                return;
            }

            var stop = (byte)0x01;
            var args = new byte[] { 0, stop, 0, 0 };
            var msg = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, args);
            await SendTcpRequest(msg);
            IQStarted = false;
            _udpClient.StopListening();
        }

        public async Task ChangeFrequencyAsync(long hz, int channel)
        {
            var channelArg = (byte)channel;
            var frequencyArg = BitConverter.GetBytes(hz).Take(5);
            var args = new[] { channelArg }.Concat(frequencyArg).ToArray();
            var msg = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverFrequency, args);
            await SendTcpRequest(msg);
        }

        // ПОМИЛКА 3 (ВИПРАВЛЕНО): Метод-обробник події екземпляра не може бути 'static'.
        private void _udpClient_MessageReceived(object? sender, byte[] e)
        {
            NetSdrMessageHelper.TranslateMessage(e, out _, out _, out _, out byte[] body);
            var samples = NetSdrMessageHelper.GetSamples(16, body);

            Console.WriteLine($"Samples recieved: " + body.Select(b => Convert.ToString(b, toBase: 16)).Aggregate((l, r) => $"{l} {r}"));

            using (FileStream fs = new FileStream("samples.bin", FileMode.Append, FileAccess.Write, FileShare.Read))
            using (BinaryWriter sw = new BinaryWriter(fs))
            {
                foreach (var sample in samples)
                {
                    sw.Write((short)sample); //write 16 bit per sample as configured 
                }
            }
        }

        // ПОМИЛКА 4 (ВИПРАВЛЕНО): Додано '?', щоб змінна могла бути null.
        private TaskCompletionSource<byte[]>? responseTaskSource;

        private async Task<byte[]> SendTcpRequest(byte[] msg)
        {
            if (!_tcpClient.Connected)
            {
                Console.WriteLine("No active connection.");
                return Array.Empty<byte>();
            }

            responseTaskSource = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            var responseTask = responseTaskSource.Task;

            await _tcpClient.SendMessageAsync(msg);

            // Використовуємо using, щоб гарантувати очищення, навіть якщо буде помилка
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5))) // Таймаут 5 секунд
            {
                var completedTask = await Task.WhenAny(responseTask, Task.Delay(-1, cts.Token));
                if (completedTask == responseTask)
                {
                    cts.Cancel(); // Скасовуємо таймаут, бо відповідь прийшла
                    return await responseTask; // Повертаємо результат
                }
                else
                {
                    // Якщо спрацював таймаут
                    responseTaskSource?.TrySetCanceled(); // Скасовуємо очікування
                    throw new TimeoutException("The request timed out.");
                }
            }
        }

        private void _tcpClient_MessageReceived(object? sender, byte[] e)
        {
            // Використовуємо локальну копію, щоб уникнути race condition
            var tcs = responseTaskSource;
            if (tcs != null)
            {
                // Це відповідь на наш запит (solicited)
                responseTaskSource = null; // Очищуємо поле перед завершенням задачі
                tcs.SetResult(e);
            }
            else
            {
                // Це незапрошене повідомлення (unsolicited)
                UnsolicitedMessageReceived?.Invoke(e);
            }
            Console.WriteLine("Response recieved: " + e.Select(b => Convert.ToString(b, toBase: 16)).Aggregate((l, r) => $"{l} {r}"));
        }
    }
}
