using NUnit.Framework;
using Microsoft.AspNetCore;
using SignalRadio.Public.Lib.Models;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SignalRadio.Public.Lib.Models.TrunkRecorder;
using System.Text;
using System.Net.WebSockets;
using System.Collections.ObjectModel;
using System.Linq;
using SignalRadio.Public.Lib.Helpers;
using SignalRadio.Web.Api.Services;
using SignalRadio.Public.Lib.Models.Enums;
using System;

namespace SignalRadio.Web.Client.Test
{
    [TestFixture]
    public class SignalRadioClientTest: WebTestBase
    {
        private SignalRadioClient _signalRadioClient;
       
        [SetUp]
        public void TestSetup()
        {
            _signalRadioClient = new SignalRadioClient(TestApiUri, TestClient);
        }

        [TearDown]
        public void TestTearDown()
        {
            _signalRadioClient = null;
        }

        [Test]
        public async Task SignalRadioClient_CanImportTalkGroups()
        {
            var tgFile = "Resources/danecom-talkgroups.priorities.csv";
            var result = await _signalRadioClient.ImportTalkgroupCsvAsync(tgFile);

            Assert.AreEqual(true, result.IsSuccessful);
            Assert.AreEqual(290, result.ItemsProcessed);
        }

        [Test]
        public async Task SignalRadioClient_CanGetTalkGroupByIdentifier()
        {
            var expectedStream = new Stream()
            {
                StreamIdentifier = "test-stream",
                LastCallTimeUtc = DateTime.UtcNow
            };

            await DbContext.Streams.AddAsync(expectedStream);
            await DbContext.SaveChangesAsync();

            var expectedTalkGroup = new TalkGroup()
            {
                Identifier = 1024,
                Mode = TalkGroupMode.Digital,
                Tag = TalkGroupTag.Hospital,
                AlphaTag = "400",
                Name = "test",
                Description = "This is a description",
                TalkGroupStreams = new Collection<TalkGroupStream>()
            };

            var existingTalkGroup = await _signalRadioClient.GetTalkGroupByIdentifierAsync(expectedTalkGroup.Identifier);

            Assert.IsNull(existingTalkGroup, "Talkgroup should not exist");

            await DbContext.TalkGroups.AddAsync(expectedTalkGroup);
            await DbContext.SaveChangesAsync();

            expectedTalkGroup.TalkGroupStreams.Add(new TalkGroupStream() { StreamId = expectedStream.Id, TalkGroupId = expectedTalkGroup.Id });

            await DbContext.SaveChangesAsync();

            var actualTalkGroup = await _signalRadioClient.GetTalkGroupByIdentifierAsync(expectedTalkGroup.Identifier);

            Assert.IsNotNull(actualTalkGroup, "TalkGroup should exist");

            Assert.AreEqual(expectedTalkGroup.Identifier, actualTalkGroup.Identifier);
            Assert.AreEqual(expectedTalkGroup.Mode, actualTalkGroup.Mode);
            Assert.AreEqual(expectedTalkGroup.Tag, actualTalkGroup.Tag);
            Assert.AreEqual(expectedTalkGroup.AlphaTag, actualTalkGroup.AlphaTag);
            Assert.AreEqual(expectedTalkGroup.Name, actualTalkGroup.Name);
            Assert.AreEqual(expectedTalkGroup.Description, actualTalkGroup.Description);

            // Assert.IsNotNull(actualTalkGroup.TalkGroupStreams);

            // Assert.AreEqual(1, expectedStream.StreamTalkGroups.Count);
        }
        
        [Test]
        public async Task SignalRadioClient_CanGetTalkGroupById()
        {
            ushort identifier = 1;
            var talkGroup = await _signalRadioClient.GetTalkGroupByIdentifierAsync(identifier);
        }

        [Test]
        public async Task SignalRadioClient_CanPostCall()
        {
            var radioCall = new RadioCall()
            {
                TalkGroupIdentifier = 123,
                CallState = 123,
                CallRecordState = 123,
                CallIdentifier = "call",
                TalkGroupTag = "abc",
                Elapsed = 123,
                Length = 123,
                IsPhase2 = true,
                IsConventional = false,
                IsEncrypted = false,
                IsAnalog = false,
                StartTime = 123,
                StopTime = 123,
                FrequencyHz = 1233123123,
                Frequency = 123,
                CallSerialNumber = 123,
                CallWavPath = "thisisthewave",
                SigmfFileName = "sigmf",
                DebugFilename = "debug",
                Filename = "filename",
                StatusFilename = "filename"
            };

            var result = await _signalRadioClient.PostCallAsync(radioCall);

        }
    }
}