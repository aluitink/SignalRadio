using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using SignalRadio.Public.Lib.Models;
using SignalRadio.Web.Client;
using Stream = SignalRadio.Public.Lib.Models.Stream;

namespace SignalRadio.LiquidBridge.Test
{
    [TestFixture]
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1()
        {
            var templateResource = (new FileInfo("Resources/stream.defaults.liq")).FullName;
            var talkgroupsResource = (new FileInfo("Resources/danecom-talkgroups.priorities.csv")).FullName;

            var liquidBridgeConfig = new LiquidBridgeConfig()
            {
                IcecastHost = "192.168.1.215",
                IcecastPort = 8000,
                StreamPassword = "Password1",
                LiquidsoapSocketsPath = Path.Join(Directory.GetCurrentDirectory(), "ls-socks"),
                LiquidsoapTemplatePath = templateResource,
                ConnectionString = "http://127.0.0.1:8001/api/"
            };

            var expectedTalkGroupIdentifier = (ushort)13050;
            var expectedCallIdentifier = (ulong)1594255860;
            var expectedFrequency = 172075000;
            var expectedOriginalExtension = "wav";
            var expectedConvertedExtension = "mp3";

            if(Directory.Exists(liquidBridgeConfig.LiquidsoapSocketsPath))
                Directory.Delete(liquidBridgeConfig.LiquidsoapSocketsPath);

            Directory.CreateDirectory(liquidBridgeConfig.LiquidsoapSocketsPath);

            var tempDir = Path.GetTempPath();

            var callWavPath = Path.Join(tempDir, string.Format("{0}-{1}_{2}.{3}", expectedTalkGroupIdentifier, expectedCallIdentifier, expectedFrequency, expectedOriginalExtension));

            if(File.Exists(callWavPath))
                File.Delete(callWavPath);

            File.Create(callWavPath).Close();

            var mockStream1 = new Stream()
            {
                Id = 1,
                StreamIdentifier = "stream-1"
            };

            var mockStream2 = new Stream()
            {
                Id = 2,
                StreamIdentifier = "stream-2"
            };

            var mockStream3 = new Stream()
            {
                Id = 3,
                StreamIdentifier = "stream-3"
            };

            var mockTalkGroup = new TalkGroup()
            {
                Id = 1,
                AlphaTag = "TGPALPHA",
                Identifier = expectedTalkGroupIdentifier,
                Name = "Tag Test Alpha (1)",
                Description = "This channel is used for testing",
                TalkGroupStreams = new Collection<TalkGroupStream>()
            };

            mockTalkGroup.TalkGroupStreams.Add(new TalkGroupStream() { TalkGroup = mockTalkGroup, TalkGroupId = mockTalkGroup.Id, Stream = mockStream1, StreamId = mockStream1.Id });
            mockTalkGroup.TalkGroupStreams.Add(new TalkGroupStream() { TalkGroup = mockTalkGroup, TalkGroupId = mockTalkGroup.Id, Stream = mockStream2, StreamId = mockStream2.Id });
            mockTalkGroup.TalkGroupStreams.Add(new TalkGroupStream() { TalkGroup = mockTalkGroup, TalkGroupId = mockTalkGroup.Id, Stream = mockStream3, StreamId = mockStream3.Id });

            var mockRadioCall = new RadioCall()
            {
                Id = 1,
                TalkGroupId = mockTalkGroup.Id,
                TalkGroup = mockTalkGroup,
                CallIdentifier = expectedCallIdentifier.ToString(),
                CallSerialNumber = (long)expectedCallIdentifier,
                CallWavPath = callWavPath,
                FrequencyHz = expectedFrequency,
                Frequency = expectedFrequency
            };

            var clientMock = new Mock<ISignalRadioClient>();
            clientMock
                .Setup(c => c.GetTalkGroupByIdentifierAsync(expectedTalkGroupIdentifier, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockTalkGroup);
            
            clientMock
                .Setup(c => c.PostCallAsync(It.IsAny<RadioCall>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockRadioCall);

            var talkGroupStreams = new Collection<Stream>(mockTalkGroup.TalkGroupStreams.Select(t => t.Stream).ToList());

            clientMock
                .Setup(c => c.GetStreamsByTalkGroupIdAsync(mockTalkGroup.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(talkGroupStreams);

            var handlerMock = new Mock<CallHandler>() { CallBase = true };
            handlerMock
                .Protected()
                .Setup<int>("ExecuteProcess", "/usr/bin/liquidsoap", ItExpr.IsAny<string>(), ItExpr.IsAny<bool>(), ItExpr.IsAny<int>())
                .Returns(0); //return 0 for success
    
            
            var handler = new CallHandler(liquidBridgeConfig, clientMock.Object);

            await handler.HandleCallAsync(callWavPath);
        }
    }
}