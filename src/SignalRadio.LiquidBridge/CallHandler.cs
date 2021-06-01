using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SignalRadio.Public.Lib.Models;
using SignalRadio.Web.Client;

namespace SignalRadio.LiquidBridge
{
    public class CallHandler
    {
        private readonly LiquidBridgeConfig _liquidBridgeConfig;
        private readonly ISignalRadioClient _client;
        
        public CallHandler(LiquidBridgeConfig liquidBridgeConfig, ISignalRadioClient client = null)
        {
            _liquidBridgeConfig = liquidBridgeConfig ?? throw new ArgumentNullException(nameof(liquidBridgeConfig));
            _client = client ?? new SignalRadioClient(new Uri(liquidBridgeConfig.ConnectionString));
        }
        public async Task HandleCallAsync(string callWavPath, string callJsonPath = null)
        {
            var radioCall = ParseCall(callWavPath, callJsonPath);
            var talkGroup = await _client.GetTalkGroupByIdentifierAsync(radioCall.TalkGroupIdentifier);

            if(talkGroup != null)
            {
                radioCall.TalkGroup = talkGroup;
                radioCall.TalkGroupId = talkGroup.Id;
            }

            radioCall = await _client.PostCallAsync(radioCall);

            ConvertCallWavToMp3(radioCall);
            var result = await PushCallToStreamAsync(radioCall);
        }
        protected RadioCall ParseCall(string callWavPath, string callJsonPath = null)
        {
            //SampleFilename
            //13050-1594255860_172075000.mp3
            ushort talkGroupId = 0;
            long callStartTimeTicks = 0;
            long callFrequencyHz = 0;

            var fileInfo = new FileInfo(callWavPath);
            var fileNameParts = fileInfo.Name.Split('-', 2, StringSplitOptions.RemoveEmptyEntries);

            if(fileNameParts.Length > 0)
                ushort.TryParse(fileNameParts[0], out talkGroupId);

            if(fileNameParts.Length > 1)
            {
                var extraParts = fileNameParts[1]?.Split('_', 2, StringSplitOptions.RemoveEmptyEntries);

                if(extraParts.Length > 0)
                    long.TryParse(extraParts[0], out callStartTimeTicks);

                if(extraParts.Length > 1)
                    long.TryParse(extraParts[1], out callFrequencyHz);
            }

            return new RadioCall()
            {
                FrequencyHz = callFrequencyHz,
                CallSerialNumber = callStartTimeTicks,
                TalkGroupIdentifier = talkGroupId,
                CallWavPath = callWavPath
            };
        }
        protected async Task<bool> PushCallToStreamAsync(RadioCall radioCall)
        {
            var tgStreams = await _client.GetStreamsByTalkGroupIdAsync(radioCall.TalkGroupId);

            if(tgStreams is null)
                return false;

            var queueCallMessage = string.Format("queue.push {0}{1}", radioCall.Filename, Environment.NewLine);
            
            foreach(var stream in tgStreams)
            {
                var mutex = new Mutex(true, $"SR_{stream.Id}");
                bool streamQueueResult = false;
                try
                {
                    mutex.WaitOne();
                    var socketPath = Path.Join(_liquidBridgeConfig.LiquidsoapSocketsPath, string.Format("{0}.sock", stream.StreamIdentifier));

                    if(!File.Exists(socketPath))
                    {
                        System.Console.WriteLine("Stream Socket missing, starting new stream...");
                        var streamConfigPath = _liquidBridgeConfig.BuildLiquidsoapConfig(stream.StreamIdentifier, stream.StreamIdentifier, radioCall?.TalkGroup?.Description, "Radio");
                        if(!await StartStreamAsync(streamConfigPath, () => File.Exists(socketPath)))
                            continue;
                    }
                    
                    streamQueueResult = await SendMessageToSocketAsync(queueCallMessage, socketPath) > 0;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                }
                finally
                {
                    mutex.ReleaseMutex();
                     if(streamQueueResult)
                        System.Console.WriteLine("{0} >> {1}", stream.StreamIdentifier, queueCallMessage);
                    else
                        System.Console.WriteLine("{0} XX Failed", stream.StreamIdentifier);
                }
            }
            return true;
        }

        protected async Task<bool> StartStreamAsync(string streamConfigPath, Func<bool> checkStreamReadyFunc)
        {
            if(!File.Exists(streamConfigPath))
                throw new ArgumentException("Stream config does not exist");

            var liquidSoapPath = "/usr/bin/liquidsoap";            
            ExecuteProcess(liquidSoapPath, streamConfigPath, false);

            var maxRetries = 20;

            byte i = 0;
            while(!checkStreamReadyFunc() && (i++ < maxRetries))
                await Task.Delay(200);
            
            return i < maxRetries;
        }

        protected async Task<int> SendMessageToSocketAsync(string message, string socketPath)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException($"'{nameof(message)}' cannot be null or empty.", nameof(message));

            if (string.IsNullOrEmpty(socketPath))
                throw new ArgumentException($"'{nameof(socketPath)}' cannot be null or empty.", nameof(socketPath));


            try
            {
                var liquidSoap = new UnixDomainSocketEndPoint(socketPath);
                using(var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                {
                    await socket.ConnectAsync(liquidSoap);
                    return await socket.SendAsync(new ReadOnlyMemory<byte>(System.Text.Encoding.UTF8.GetBytes(message)), SocketFlags.None);
                }
            }
            catch(Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
                return -1;
            }
        }

        protected virtual void ConvertCallWavToMp3(RadioCall radioCall)
        {
            if (radioCall is null)
                throw new ArgumentNullException(nameof(radioCall));

            var title = string.Format("[{0}][{1}]", radioCall.CallSerialNumber, radioCall?.TalkGroup?.AlphaTag);
            var artist = radioCall?.TalkGroup?.Identifier;
            var comment = string.Format("{0} - {1}", radioCall?.TalkGroup?.Tag, radioCall?.TalkGroup?.Description);

            var inputFile = new FileInfo(radioCall.CallWavPath);

            if(!inputFile.Exists)
                throw new Exception("Missing input file");

            var tempDir = Path.GetTempPath();
            var outputPath = Path.Join(tempDir, string.Format("{0}.mp3", inputFile.Name.TrimEnd(inputFile.Extension.ToArray())));

            var lamePath = "/usr/bin/lame";
            var lameArgs = string.Format("--quiet --preset voice --tt \"{0}\" --ta \"{1}\", --tc \"{2}\" {3} {4}", title, artist, comment, inputFile.FullName, outputPath);
            
            //Execute Lame to convert, timeout after 20 seconds, success on result code 0
            if(ExecuteProcess(lamePath, lameArgs, true, 20 * 1000) == 0)
            {
                radioCall.Filename = outputPath;
                System.Console.WriteLine("Converted call to mp3: {0}", radioCall.Filename);
            }            
        }

        protected virtual int ExecuteProcess(string fileName, string arguments, bool waitForExit = true, int waitForExitTimeoutMsec = int.MaxValue)
        {
            var proc = Process.Start(fileName, arguments);

            if(waitForExit)
                return proc.WaitForExit(waitForExitTimeoutMsec) ? proc.ExitCode : int.MinValue;
            
            return 1;
        }
    }
}
