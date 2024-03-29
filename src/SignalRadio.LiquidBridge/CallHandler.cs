using System;
using System.Collections.Concurrent;
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
        protected ConcurrentDictionary<string, Socket> SocketPool { get; set; } = new ConcurrentDictionary<string, Socket>();

        private readonly LiquidBridgeConfig _liquidBridgeConfig;
        private readonly ISignalRadioClient _client;
        
        public CallHandler(LiquidBridgeConfig liquidBridgeConfig, ISignalRadioClient client = null)
        {
            _liquidBridgeConfig = liquidBridgeConfig ?? throw new ArgumentNullException(nameof(liquidBridgeConfig));
            _client = client ?? new SignalRadioClient(new Uri(liquidBridgeConfig.ConnectionString));
        }
        public async Task HandleCallAsync(string callWavPath, string callJsonPath = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(callWavPath))
                throw new ArgumentException($"'{nameof(callWavPath)}' cannot be null or empty.", nameof(callWavPath));
            
            if (!File.Exists(callWavPath))
                throw new ArgumentException($"'{callWavPath}' does not exist.", nameof(callWavPath));

            cancellationToken.ThrowIfCancellationRequested();

            var radioCall = await ParseCallAsync(_client, callWavPath, callJsonPath);
            radioCall = await _client.PostCallAsync(radioCall, cancellationToken);
            
            System.Console.WriteLine("Received Call: {0}", radioCall.ToString());

            ConvertCallWavToMp3(radioCall);
            var result = await PushCallToStreamAsync(radioCall, cancellationToken);
        }
        protected async Task<RadioCall> ParseCallAsync(ISignalRadioClient client, string callWavPath, string callJsonPath = null, CancellationToken cancellationToken = default(CancellationToken))
        {

            //SampleFilename
            //13050-1594255860_172075000.mp3
            ushort talkGroupIdentifier = 0;
            ulong callStartTimeTicks = 0;
            long callFrequencyHz = 0;

            var fileInfo = new FileInfo(callWavPath);
            var fileNameParts = fileInfo.Name.Split('-', 2, StringSplitOptions.RemoveEmptyEntries);

            if(fileNameParts.Length > 0)
                ushort.TryParse(fileNameParts[0], out talkGroupIdentifier);

            var talkGroup = await _client.GetTalkGroupByIdentifierAsync(talkGroupIdentifier, cancellationToken);

            if (fileNameParts.Length > 1)
            {
                var extraParts = fileNameParts[1]?.Split('_', 2, StringSplitOptions.RemoveEmptyEntries);

                if(extraParts.Length > 0)
                    ulong.TryParse(extraParts[0], out callStartTimeTicks);

                if(extraParts.Length > 1)
                    long.TryParse(extraParts[1], out callFrequencyHz);
            }

            return new RadioCall()
            {
                Frequency = callFrequencyHz,
                CallIdentifier = callStartTimeTicks.ToString(),
                CallSerialNumber = (long)callStartTimeTicks,
                StartTime = DateTimeFromFileTime((long)callStartTimeTicks),
                CallWavPath = callWavPath,
                TalkGroupId = talkGroup?.Id ?? 0
            };
        }
        protected async Task<bool> PushCallToStreamAsync(RadioCall radioCall, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tgStreams = await _client.GetStreamsByTalkGroupIdAsync(radioCall.TalkGroupId, cancellationToken);

            if(tgStreams is null)
                return false;

            var queueCallMessage = string.Format("queue.push {0}{1}", radioCall.CallWavPath, Environment.NewLine);
            
            foreach (var stream in tgStreams)
            {
                using(var mutex = new Mutex(true, $"SR_{stream.Id}"))
                {
                    bool streamQueueResult = false;
                    try
                    {
                        mutex.WaitOne();
                        var socketPath = Path.Join(_liquidBridgeConfig.LiquidsoapSocketsPath, string.Format("{0}.sock", stream.StreamIdentifier));

                        if(!File.Exists(socketPath))
                        {
                            System.Console.WriteLine("Stream Socket missing, starting new stream...");
                            var streamConfigPath = _liquidBridgeConfig.BuildLiquidsoapConfig(stream.StreamIdentifier, stream.StreamIdentifier, radioCall?.TalkGroup?.Description, "Radio");
                            if(!await StartStreamAsync(streamConfigPath, () => File.Exists(socketPath), cancellationToken))
                                continue;
                        }

                        streamQueueResult = SendMessageToSocket(queueCallMessage, socketPath, cancellationToken) > 0;
                    }
                    catch(OperationCanceledException)
                    {
                        return true;
                    }
                    catch(Exception e)
                    {
                        System.Console.WriteLine(e);
                    }
                    finally
                    {
                        if(streamQueueResult)
                            System.Console.WriteLine("{0} >> {1}", stream.StreamIdentifier, queueCallMessage);
                        else
                            System.Console.WriteLine("{0} XX Failed", stream.StreamIdentifier);
                    }
                }
            }
            return true;
        }
        protected async Task<bool> StartStreamAsync(string streamConfigPath, Func<bool> checkStreamReadyFunc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if(!File.Exists(streamConfigPath))
                throw new ArgumentException("Stream config does not exist");

            cancellationToken.ThrowIfCancellationRequested();

            var liquidSoapPath = "/usr/bin/liquidsoap";            
            ExecuteProcess(liquidSoapPath, streamConfigPath, false);

            var maxRetries = 20;

            byte i = 0;
            while(!checkStreamReadyFunc() && (i++ < maxRetries) && !cancellationToken.IsCancellationRequested)
                await Task.Delay(200, cancellationToken);
            
            return i < maxRetries;
        }
        protected int SendMessageToSocket(string message, string socketPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException($"'{nameof(message)}' cannot be null or empty.", nameof(message));

            if (string.IsNullOrEmpty(socketPath))
                throw new ArgumentException($"'{nameof(socketPath)}' cannot be null or empty.", nameof(socketPath));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var socket = SocketPool.GetOrAdd(socketPath, (path) => {
                    var s = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);                    
                    s.Connect(new UnixDomainSocketEndPoint(socketPath));
                    return s;
                });

                var bytesSent = socket.Send(System.Text.Encoding.UTF8.GetBytes(message));

                if(socket.Available > 0)
                {
                    var buff = new byte[socket.Available];
                    var bytesReceived = socket.Receive(buff);
                    Console.WriteLine("Unix socket received {0} bytes", bytesReceived);
                    if(bytesReceived > 0)
                    {
                        System.Console.WriteLine(System.Text.Encoding.UTF8.GetString(buff));
                    }
                }
                return bytesSent;
            }
            catch(Exception ex)
            {
                System.Console.WriteLine("SendMessageToSocket: {0}",ex.ToString());
                return -1;
            }
        }
        protected virtual void ConvertCallWavToMp3(RadioCall radioCall, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (radioCall is null)
                throw new ArgumentNullException(nameof(radioCall));

            var inputFile = new FileInfo(radioCall.CallWavPath);

            if(!inputFile.Exists)
                throw new Exception("Missing input file");

            
            var tempDir = Path.GetTempPath();
            var outputPath = Path.Join(tempDir, string.Format("{0}.mp3", inputFile.Name.TrimEnd(inputFile.Extension.ToArray())));

            string title = string.Empty;
            string artist = string.Empty;
            string comment = string.Empty;

            if(!GetMetadata(radioCall, out title, out artist, out comment))
                throw new Exception("Could not GetMetadata");

            var lamePath = "/usr/bin/lame";
            var lameArgs = string.Format("--quiet --preset voice --tt \"{0}\" --ta \"{1}\", --tc \"{2}\" {3} {4}", title, artist, comment, inputFile.FullName, outputPath);
            
            //Execute Lame to convert, timeout after 20 seconds, success on result code 0
            if(ExecuteProcess(lamePath, lameArgs, true, 20 * 1000) == 0)
            {
                radioCall.CallWavPath = outputPath;
                System.Console.WriteLine("Converted call to mp3: {0}", radioCall.CallWavPath);
            }
        }
        protected virtual bool GetMetadata(RadioCall radioCall, out string title, out string artist, out string comment)
        {
            var talkGroup = radioCall.TalkGroup;
            if(radioCall.TalkGroup is null)
                throw new Exception("Unable to determine TalkGroup");

            title = null;
            artist = null;
            comment = null;

            try
            {
                title = string.Format("[{0}][{1}]", radioCall.CallSerialNumber, talkGroup.AlphaTag);
                artist = string.Format("{0} ({1})", talkGroup.Identifier, talkGroup.Name);
                comment = talkGroup.Description;
                return true;              
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        protected virtual int ExecuteProcess(string fileName, string arguments, bool waitForExit = true, int waitForExitTimeoutMsec = int.MaxValue)
        {
            var proc = Process.Start(fileName, arguments);

            if(waitForExit)
                return proc.WaitForExit(waitForExitTimeoutMsec) ? proc.ExitCode : int.MinValue;
            
            return 1;
        }
        private DateTime DateTimeFromFileTime(long fileTime)
        {
            return new DateTime(1970, 1, 1).ToUniversalTime().AddSeconds(fileTime);
        }
    }
}
