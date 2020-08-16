using System;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SignalRadio.Public.Lib.Models;
using SignalRadio.Web.Client;

namespace SignalRadio.LiquidBridge
{
    class Program
    {
        private static SignalRadioClient _client;
        private static LiquidBridgeConfig _liquidConfig;
        static void Main(string[] args)
        {
            bool isHelpRequested = IsArgumentFlagExists(args, "help", "-help", "--help", "?", "/?", "-?");
            
            if (isHelpRequested || args.Length < 2)
            {
                System.Console.WriteLine(args[0]);
                System.Console.WriteLine(args[1]);
                System.Console.WriteLine("DeEerR I'm thE OpPeraToR!");
                PrintUsage();
                return;
            }
            
            try
            {
                _liquidConfig = GetArgumentValue(args, "config", (s) => {

                    var config = LoadConfigFromFile<LiquidBridgeConfig>((new FileInfo(s)).FullName);
                    //QualifyPaths
                    config.LiquidsoapSocketsPath = (new FileInfo(config.LiquidsoapSocketsPath)).FullName;
                    config.LiquidsoapTemplatePath = (new FileInfo(config.LiquidsoapTemplatePath)).FullName;
                    config.TalkGroupCsvPath = (new FileInfo(config.TalkGroupCsvPath)).FullName;

                    return config;
                });

                _client = new SignalRadioClient(_liquidConfig.ConnectionString);
                
                if(IsArgumentFlagExists(args, "import"))
                {
                    var results = _client.ImportTalkgroupCsvAsync(_liquidConfig.TalkGroupCsvPath).Result;
                }

                var callWavPath = args[1];

                if(string.IsNullOrEmpty(callWavPath))
                    throw new Exception("Invalid callWavPath :(");

                HandleCall(callWavPath);
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }
        private static void HandleCall(string callWavPath)
        {
            var call = ParseCall(callWavPath);
            
            var talkGroup = _client.GetTalkGroupByIdentifierAsync(call.TalkGroupIdentifier).Result;

            if(talkGroup != null)
            {
                call.TalkGroup = talkGroup;
                call.TalkGroupId = talkGroup.Id;
            }

            System.Console.WriteLine("Received call: {0}", call);

            call = _client.PostCallAsync(call, CancellationToken.None).Result;

            if(call?.TalkGroup?.TalkGroupStreams?.Any() != null)
            {
                ConvertWavToMp3(call);
                PushCallToStreams(call);
            }
            else
            {
                System.Console.WriteLine("No talkgroup or streams not found.");
            }
        }

        private static T LoadConfigFromFile<T>(string filePath)
        {
            using(var stream = new FileStream(filePath, FileMode.Open))
            {
                var configBytes = new byte[stream.Length];
                var bytesRead = stream.Read(configBytes, 0, configBytes.Length);
                return (T)JsonSerializer.Deserialize<T>(new ReadOnlySpan<byte>(configBytes, 0, configBytes.Length));
            }
        }
        private static bool PushCallToStreams(RadioCall call)
        {
            var queueCallMessage = string.Format("queue.push {0}{1}", call.Filename, Environment.NewLine);
            foreach(var tgs in call.TalkGroup.TalkGroupStreams)
            {
                var stream = tgs.Stream;
                var mutex = new Mutex(true, $"SR_{stream.Id}");
                bool streamQueueResult = false;
                try
                {
                    mutex.WaitOne();
                    var socketPath = Path.Join(_liquidConfig.LiquidsoapSocketsPath, string.Format("{0}.sock", stream.StreamIdentifier));
                    if(!File.Exists(socketPath))
                    {
                        System.Console.WriteLine("PCTS: Socket Missing, Start Stream");

                        var streamConfigPath = _liquidConfig.BuildLiquidsoapConfig(stream.StreamIdentifier, stream.StreamIdentifier, call.TalkGroup.Description, "Radio");
                        if(!StartStream(streamConfigPath, () => File.Exists(socketPath)))
                            continue;
                    }
                    
                    streamQueueResult = SendMessageToSocket(queueCallMessage, socketPath) > 0;
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
        private static bool StartStream(string streamConfigPath, Func<bool> checkStreamReadyFunc)
        {
            if(!File.Exists(streamConfigPath))
                throw new ArgumentException("Stream config does not exist");

            var streamProc = Process.Start("/usr/bin/liquidsoap", streamConfigPath);

            byte i = 0;
            while(!checkStreamReadyFunc() && !streamProc.HasExited && (i++ < 20))
                Task.Delay(200).Wait();
            
            return !streamProc.HasExited;
        }
        private static int SendMessageToSocket(string message, string socketPath)
        {
            try
            {
                
                var liquidSoap = new UnixDomainSocketEndPoint(socketPath);
                using(var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                {
                    socket.Connect(liquidSoap);
                    return socket.Send(System.Text.Encoding.UTF8.GetBytes(message));
                }
            }
            catch(Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
                return -1;
            }
        }
        private static TalkGroup GetTalkGroupById(string talkGroupCsvPath, int talkGroupId)
        {
            using(var csvStream = new StreamReader(talkGroupCsvPath))
            {
                while(!csvStream.EndOfStream)
                {
                    var line = csvStream.ReadLine();
                    var lineParts = line.Split(',');

                    ushort tgId = 0;
                    ushort priority = 0;
                    string hexId = null;
                    string mode = null;
                    string alphaTag = null;
                    string tgName = null;
                    string tgType = null;
                    string tgCategory = null;
                    string streamIds = null;
                    string[] streams = null;
                    if(lineParts.Length > 0)
                        if(!ushort.TryParse(lineParts[0], out tgId))
                            continue;

                    if(tgId != talkGroupId)
                        continue;

                    if(lineParts.Length > 1)
                        streamIds = lineParts[1];
                    if(lineParts.Length > 2)
                        mode = lineParts[2];
                    if(lineParts.Length > 3)
                        alphaTag = lineParts[3];
                    if(lineParts.Length > 4)
                        tgName = lineParts[4];
                    if(lineParts.Length > 5)
                        tgType = lineParts[5];
                    if(lineParts.Length > 6)
                        tgCategory = lineParts[6];
                    if(lineParts.Length > 7)
                        ushort.TryParse(lineParts[7], out priority);

                    if(streamIds != null)
                        streams = streamIds.Split('|');

                    return new TalkGroup()
                    {
                        Identifier = tgId,
                        Priority = priority,
                        Mode = mode,
                        AlphaTag = alphaTag,
                        Name = tgName,
                        Tag = tgType,
                        Description = tgCategory,
                        Streams = streams
                    };
                }

                return null;
            }
        }
        private static void ConvertWavToMp3(RadioCall call)
        {
            var title = string.Format("[{0}][{1}]", call.CallSerialNumber, call.TalkGroup.AlphaTag);
            var artist = call.TalkGroup.Identifier;
            var comment = string.Format("{0} - {1}", call.TalkGroup.Tag, call.TalkGroup.Description);

            var inputFile = new FileInfo(call.CallWavPath);

            if(!inputFile.Exists)
                throw new Exception("Missing input file");

            var tempDir = Path.GetTempPath();
            var outputPath = Path.Join(tempDir, string.Format("{0}.mp3", inputFile.Name.TrimEnd(inputFile.Extension.ToArray())));
            
            var lameProcess = Process.Start("/usr/bin/lame", 
            string.Format("--quiet --preset voice --tt \"{0}\" --ta \"{1}\", --tc \"{2}\" {3} {4}", title, artist, comment, inputFile.FullName, outputPath));
            lameProcess.WaitForExit();

            if(lameProcess.ExitCode == 0)
            {
                call.Filename = outputPath;
                System.Console.WriteLine("Converted call to mp3: {0}", call.Filename);
            }
        }
        private static RadioCall ParseCall(string callWavPath)
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
        private static T GetArgumentValue<T>(string[] args, string arg, Func<string, T> retFunc, T defaultValue = default(T))
        {
            if (args != null && args.Length > 0)
            {
                var argVal = args.SingleOrDefault(a => a.StartsWith(arg, StringComparison.InvariantCultureIgnoreCase));
                if (!string.IsNullOrEmpty(argVal))
                    return retFunc(argVal.Replace(string.Format("{0}:", arg), string.Empty));
            }

            return defaultValue;
        }
        private static bool IsArgumentFlagExists(string[] args, params string[] flagValues)
        {
            if (args != null && args.Length > 0)
            {
                if (flagValues == null || flagValues.Length == 0)
                    throw new ArgumentNullException("flagValues", "Must pass flagValues to search for");

                return args.Any(a =>
                    flagValues.Any(f =>
                        f.Equals(a, StringComparison.InvariantCultureIgnoreCase)
                    )
                );
            }

            return false;
        }
        private static void PrintUsage()
        {
            var binName = AppDomain.CurrentDomain.FriendlyName;

            Console.WriteLine("{0} - Command Line Usage", "LiquidBridge");
            Console.WriteLine(" Command line arguments are separated by a space character.");
            Console.WriteLine(" Argument values containing a space should be surrounded by double quotes. e.g. \"ARG:Value With Space\"");
            Console.WriteLine(" Arguments and values are separated by colon character. e.g. ARG:VAL");
            Console.WriteLine();
            Console.WriteLine("{0} Config:config.json call.wav", binName);
            Console.WriteLine();
            Console.WriteLine("{0} Help", binName);
            Console.WriteLine();
            Console.WriteLine(" Help - Display this usage statement");
            Console.WriteLine();
        }
    }
}
