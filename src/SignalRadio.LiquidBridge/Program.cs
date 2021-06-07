using System;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using SignalRadio.Web.Client;
using System.Threading.Tasks;
using System.Threading;

namespace SignalRadio.LiquidBridge
{
    class Program
    {
        private static LiquidBridgeConfig _liquidConfig;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        static void Main(string[] args)
        {
            bool isHelpRequested = IsArgumentFlagExists(args, "help", "-help", "--help", "?", "/?", "-?");
            
            if (isHelpRequested || args.Length < 2) //config and mode are required
            {
                PrintUsage();
                return;
            }
            
            try
            {
                _liquidConfig = GetArgumentValue(args, "config", (s) => {
                    var configPath =(new FileInfo(s)).FullName;
                    var config = LoadConfigFromFile<LiquidBridgeConfig>(configPath);
                    config.LiquidsoapSocketsPath = (new FileInfo(config.LiquidsoapSocketsPath)).FullName;
                    config.LiquidsoapTemplatePath = (new FileInfo(config.LiquidsoapTemplatePath)).FullName;
                    return config;
                });

                if(_liquidConfig is null)
                    throw new ArgumentException("config is required.", "config");

                var mode = GetArgumentValue<Mode>(args, "mode", (s) => {
                    Mode mode = Mode.None;
                    if(!Enum.TryParse<Mode>(s, true, out mode))
                        throw new ArgumentOutOfRangeException("mode");
                    return mode;
                });

                var callWavPath = GetArgumentValue(args, "wav", (s) => {
                    return (new FileInfo(s)).FullName;
                });
                var callJsonPath = GetArgumentValue(args, "json", (s) => {
                    return (new FileInfo(s)).FullName;
                });
                var importCsvPath = GetArgumentValue(args, "csv", (s) => {
                    return (new FileInfo(s)).FullName;
                });

                switch(mode)
                {
                    case Mode.Direct:
                        HandleDirectMode(callWavPath, callJsonPath, _cancellationTokenSource.Token);
                        break;
                    case Mode.Client:
                        HandleClientMode(callWavPath, callJsonPath, _cancellationTokenSource.Token);
                        break;
                    case Mode.Server:
                        HandleServerMode();
                        break;
                    case Mode.Import:
                        HandleImportMode(importCsvPath);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("mode");
                }
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }
        private static void HandleDirectMode(string callWavPath, string callJsonPath = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var callHandler = new CallHandler(_liquidConfig);
            callHandler.HandleCallAsync(callWavPath, callJsonPath, _cancellationTokenSource.Token)
                .GetAwaiter()
                    .GetResult();
        }
        private static void HandleClientMode(string callWavPath, string callJsonPath = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var messageToSend = string.Format("{0}{2}{1}", 
                callWavPath, //Audio
                callJsonPath, //Additional Data
                callJsonPath != null ? "|" : null); //Delimiter

            using(var clientSocket = new UdpSocket())
            {
                clientSocket.Client(_liquidConfig.UdpServerIpAddress, _liquidConfig.UdpServerPort);
                clientSocket.Send(messageToSend);
            }
        }
        private static void HandleImportMode(string csvPath)
        {
            if (csvPath is null)
                throw new ArgumentNullException(nameof(csvPath));
            
            if(!File.Exists(csvPath))
                throw new ArgumentException($"'{csvPath}' does not exist.");
            
            var results = (new SignalRadioClient(new Uri(_liquidConfig.ConnectionString)))
                .ImportTalkgroupCsvAsync(csvPath)
                    .GetAwaiter()
                        .GetResult();
        }
        private static void HandleServerMode()
        {
            var callHandler = new CallHandler(_liquidConfig);
            using(var socket = new UdpSocket())
            {
                try
                {
                    socket.Server(_liquidConfig.UdpServerIpAddress, _liquidConfig.UdpServerPort, async (msg) => 
                    {
                        var parts = msg.Split('|', 2, StringSplitOptions.RemoveEmptyEntries);

                        var callWavPath = string.Empty;
                        var callJsonPath = string.Empty;

                        if(parts.Length > 0)
                        {
                            callWavPath = parts[0];
                            if(parts.Length > 1)
                                callJsonPath = parts[1];
                        }

                        await callHandler.HandleCallAsync(callWavPath, callJsonPath, _cancellationTokenSource.Token);  
                    });

                    while(!_cancellationTokenSource.IsCancellationRequested)
                        Task.Delay(200).Wait(_cancellationTokenSource.Token);
                }
                finally
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            Console.WriteLine("Press Enter to Exit");
            Console.ReadLine();
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
            Console.WriteLine(" Config - LiquidBridge configuration file [REQUIRED]");
            Console.WriteLine(" Mode - Operating Mode [REQUIRED]");
            Console.WriteLine("   Client - Send request to LiquidBridge UDP Server.");
            Console.WriteLine("     Wav - WAV file to process and queue. [REQUIRED]");
            Console.WriteLine("     Json - Json file to for additional data. [Optional]");
            Console.WriteLine("   Direct - Send request directly to Liquidsoap Socket");
            Console.WriteLine("     Wav - WAV file to process and queue. [REQUIRED]");
            Console.WriteLine("     Json - Json file to for additional data. [Optional]");
            Console.WriteLine("   Server - Starts LiquidBridge UDP Server.");
            Console.WriteLine("   Import - SignalRadio Talk Group CSV Import");
            Console.WriteLine("     Csv - TalkGroups CSV file. [REQUIRED]");
            Console.WriteLine();
            Console.WriteLine("{0} Config:config.json Mode:direct Wav:13050-1594255860_172075000.wav Json:13050-1594255860_172075000.json", binName);
            Console.WriteLine();
            Console.WriteLine("{0} Config:config.json Mode:server", binName);
            Console.WriteLine();
            Console.WriteLine("{0} Config:config.json Mode:client Wav:13050-1594255860_172075000.wav Json:13050-1594255860_172075000.json", binName);
            Console.WriteLine();
            Console.WriteLine("{0} Config:config.json Mode:import Csv:talkgroups.csv", binName);
            Console.WriteLine();
            Console.WriteLine("{0} Help", binName);
            Console.WriteLine();
            Console.WriteLine(" Help - Display this usage statement");
            Console.WriteLine();
        }
    }
}
