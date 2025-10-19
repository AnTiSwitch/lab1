using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NetSdrClientApp
{
    public class NetSdrClient
    {
        // ВИПРАВЛЕНО: 'readonly' тепер стоїть перед типом
        private readonly ITcpClient _tcpClient;
        private readonly IUdpClient _udpClient;

        public bool IQStarted { get; private set; }

        private TaskCompletionSource<byte[]>? _responseTaskSource;

        public NetSdrClient(ITcpClient tcpClient, IUdpClient udpClient)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));

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
                    await SendTcpRequestAsync(msg);
                }
            }
        }

        // ВИПРАВЛЕНО: Опечатка в назві методу
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

            await SendTcpRequestAsync(msg);

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

            await SendTcpRequestAsync(msg);

            IQStarted = false;

            _udpClient.StopListening();
        }

        public async Task ChangeFrequencyAsync(long hz, int channel)
        {
            var channelArg = (byte)channel;
            var frequencyArg = BitConverter.GetBytes(hz).Take(5);
            var args = new[] { channelArg }.Concat(frequencyArg).ToArray();

            var msg = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverFrequency, args);

            await SendTcpRequestAsync(msg);
        }

        private void _udpClient_MessageReceived(object? sender, byte[] e)
        {
            NetSdrMessageHelper.TranslateMessage(e, out MsgTypes type, out ControlItemCodes code, out ushort sequenceNum, out byte[] body);
            var samples = NetSdrMessageHelper.GetSamples(16, body);

            Console.WriteLine($"Samples recieved: " + string.Join(" ", body.Select(b => b.ToString("X2"))));

            using (FileStream fs = new FileStream("samples.bin", FileMode.Append, FileAccess.Write, FileShare.Read))
            using (BinaryWriter sw = new BinaryWriter(fs))
            {
                foreach (var sample in samples)
                {
                    sw.Write((short)sample); //write 16 bit per sample as configured 
                }
            }
        }

        private async Task<byte[]?> SendTcpRequestAsync(byte[] msg)
        {
            if (!_tcpClient.Connected)
            {
                Console.WriteLine("No active connection.");
                // ВИПРАВЛЕНО: Повертаємо null, але асинхронно
                return null;
            }

            _responseTaskSource = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            await _tcpClient.SendMessageAsync(msg);

            // Очікуємо на відповідь з таймаутом, щоб уникнути вічного очікування
            var completedTask = await Task.WhenAny(_responseTaskSource.Task, Task.Delay(5000)); // 5 секунд таймаут

            if (completedTask == _responseTaskSource.Task)
            {
                return await _responseTaskSource.Task;
            }
            else
            {
                Console.WriteLine("Request timed out.");
                _responseTaskSource.TrySetCanceled();
                return null;
            }
        }

        private void _tcpClient_MessageReceived(object? sender, byte[] e)
        {
            //TODO: add Unsolicited messages handling here
            _responseTaskSource?.TrySetResult(e);
            Console.WriteLine("Response recieved: " + string.Join(" ", e.Select(b => b.ToString("X2"))));
        }
    }
}
