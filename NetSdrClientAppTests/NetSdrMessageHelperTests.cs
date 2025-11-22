using NetSdrClientApp.Messages;
using NUnit.Framework;
using System;
using System.Linq;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        private const short _maxMessageLength = 8191;
        private const short _maxDataItemMessageLength = 8194;
        private const short _msgHeaderLength = 2;

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));
            Assert.That(actualCode, Is.EqualTo((short)code));
            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));
            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetControlItemMessage_NoControlItemCode_UsesGetMessageBranch()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            byte[] parameters = new byte[] { 0x01, 0x02 };

            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            Assert.That(msg.Length, Is.EqualTo(4));
            Assert.That(msg.Skip(_msgHeaderLength).Count(), Is.EqualTo(parameters.Length));
        }

        [Test]
        public void GetControlItemMessage_MaxMessageLength_ShouldPass()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            int parametersLength = _maxMessageLength - _msgHeaderLength - 2;

            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            Assert.That(msg.Length, Is.EqualTo(_maxMessageLength));
        }

        [Test]
        public void GetControlItemMessage_ExceedsMaxMessageLength_ThrowsException()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            int invalidParametersLength = _maxMessageLength - _msgHeaderLength - 2 + 1;

            Assert.Throws<ArgumentException>(() =>
            {
                NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[invalidParametersLength]);
            }, "Message length exceeds allowed value");
        }

        [Test]
        public void GetDataItemMessage_MaxDataItemMessageLength_SetsHeaderToZero()
        {
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            int parametersLength = _maxDataItemMessageLength - _msgHeaderLength;

            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            Assert.That(msg.Length, Is.EqualTo(_maxDataItemMessageLength));

            var num = BitConverter.ToUInt16(msg.Take(2).ToArray());
            var lengthFromHeader = num - ((int)type << 13);

            Assert.That(lengthFromHeader, Is.EqualTo(0), "Header length should be 0 for max DataItem length");
        }

        [Test]
        public void TranslateMessage_ControlItem_Success()
        {
            var expectedType = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var expectedItemCode = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            byte[] expectedBody = { 0xAA, 0xBB, 0xCC };

            byte[] rawMsg = NetSdrMessageHelper.GetControlItemMessage(expectedType, expectedItemCode, expectedBody);

            bool success = NetSdrMessageHelper.TranslateMessage(rawMsg, out var actualType, out var actualItemCode, out var sequenceNumber, out var actualBody);

            Assert.IsTrue(success);
            Assert.That(actualType, Is.EqualTo(expectedType));
            Assert.That(actualItemCode, Is.EqualTo(expectedItemCode));
            Assert.That(sequenceNumber, Is.EqualTo(0));
            Assert.That(actualBody, Is.EqualTo(expectedBody));
        }

        [Test]
        public void TranslateMessage_ControlItem_InvalidCode_Fails()
        {
            var expectedType = NetSdrMessageHelper.MsgTypes.SetControlItem;
            byte[] validBody = { 0xAA, 0xBB, 0xCC };
            byte[] rawMsg = NetSdrMessageHelper.GetControlItemMessage(expectedType, NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency, validBody);

            ushort invalidCode = 0xFFFF;
            byte[] invalidCodeBytes = BitConverter.GetBytes(invalidCode);
            rawMsg[_msgHeaderLength] = invalidCodeBytes[0];
            rawMsg[_msgHeaderLength + 1] = invalidCodeBytes[1];

            bool success = NetSdrMessageHelper.TranslateMessage(rawMsg,
                                                               out var actualType,
                                                               out var actualItemCode,
                                                               out var sequenceNumber,
                                                               out var actualBody);

            Assert.IsFalse(success);
            Assert.That(actualType, Is.EqualTo(expectedType));
            Assert.That(actualItemCode, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.None));
            Assert.That(actualBody.Length, Is.EqualTo(validBody.Length + 2));
        }

        [Test]
        public void TranslateMessage_DataItem_Success()
        {
            var expectedType = NetSdrMessageHelper.MsgTypes.DataItem3;
            ushort expectedSequenceNumber = 12345;
            byte[] expectedBody = { 0xAA, 0xBB, 0xCC };

            int totalPayloadLength = 5;

            ushort headerValue = (ushort)(totalPayloadLength + ((int)expectedType << 13));
            byte[] headerBytes = BitConverter.GetBytes(headerValue);

            byte[] seqBytes = BitConverter.GetBytes(expectedSequenceNumber);

            byte[] rawMsg = headerBytes
                                 .Concat(seqBytes)
                                 .Concat(expectedBody)
                                 .ToArray();

            bool success = NetSdrMessageHelper.TranslateMessage(rawMsg,
                                                               out var actualType,
                                                               out var itemCode,
                                                               out var actualSequenceNumber,
                                                               out var actualBody);

            Assert.IsTrue(success);

            Assert.That(actualType, Is.EqualTo(expectedType));
            Assert.That(actualBody, Is.EqualTo(expectedBody));

            Assert.That(itemCode, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.None));
            Assert.That(actualSequenceNumber, Is.EqualTo(expectedSequenceNumber));
        }

        [Test]
        public void TranslateMessage_DataItem_MaxMessageLength_Success()
        {
            var expectedType = NetSdrMessageHelper.MsgTypes.DataItem0;
            byte[] expectedBody = new byte[_maxDataItemMessageLength - _msgHeaderLength];

            ushort headerValue = (ushort)((int)expectedType << 13);
            byte[] rawMsg = BitConverter.GetBytes(headerValue).Concat(expectedBody).ToArray();

            NetSdrMessageHelper.TranslateMessage(rawMsg, out var actualType, out var itemCode, out var sequenceNumber, out var actualBody);

            Assert.That(actualType, Is.EqualTo(expectedType));
            Assert.That(rawMsg.Length, Is.EqualTo(_maxDataItemMessageLength));
        }

        [Test]
        public void GetSamples_SampleSizeTooBig_ThrowsException()
        {
            ushort sampleSizeBits = 40;
            byte[] body = { 0x01, 0x02, 0x03, 0x04, 0x05 };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                NetSdrMessageHelper.GetSamples(sampleSizeBits, body).ToList();
            });
        }

        [TestCase((ushort)8, 1)]
        [TestCase((ushort)16, 2)]
        [TestCase((ushort)24, 3)]
        [TestCase((ushort)32, 4)]
        public void GetSamples_CorrectlyTranslatesSingleSample(ushort sampleSizeBits, int sampleSizeBytes)
        {
            byte[] body = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var samples = NetSdrMessageHelper.GetSamples(sampleSizeBits, body).ToList();

            int expectedSampleCount = body.Length / sampleSizeBytes;

            Assert.That(samples.Count, Is.EqualTo(expectedSampleCount));

            byte[] actualBytes = BitConverter.GetBytes(samples.First());

            for (int i = 0; i < sampleSizeBytes; i++)
            {
                Assert.That(actualBytes[i], Is.EqualTo(body[i]));
            }

            for (int i = sampleSizeBytes; i < 4; i++)
            {
                Assert.That(actualBytes[i], Is.EqualTo(0));
            }
        }

        [Test]
        public void GetSamples_TranslatesMultipleSamples()
        {
            ushort sampleSizeBits = 16;
            byte[] body = { 0x01, 0x00, 0x02, 0x00, 0x03, 0x00 };

            var samples = NetSdrMessageHelper.GetSamples(sampleSizeBits, body).ToList();

            Assert.That(samples.Count, Is.EqualTo(3));

            Assert.That(samples[0], Is.EqualTo(BitConverter.ToInt32(new byte[] { 0x01, 0x00, 0x00, 0x00 })));
            Assert.That(samples[1], Is.EqualTo(BitConverter.ToInt32(new byte[] { 0x02, 0x00, 0x00, 0x00 })));
            Assert.That(samples[2], Is.EqualTo(BitConverter.ToInt32(new byte[] { 0x03, 0x00, 0x00, 0x00 })));
        }
    }
}