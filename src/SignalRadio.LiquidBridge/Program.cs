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
        private static SignalRadioClient _client;
        private static LiquidBridgeConfig _liquidConfig;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        static void Main(string[] args)
        {
            bool isHelpRequested = IsArgumentFlagExists(args, "help", "-help", "--help", "?", "/?", "-?");
            
            if (isHelpRequested || args.Length < 2)
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

                var isServer = IsArgumentFlagExists(args, "server");

                if(isServer)
                {
                    StartUdpServer(_cancellationTokenSource.Token);

                    Console.WriteLine("Press Enter to Exit");
                    Console.ReadLine();
                }

                var importResults = GetArgumentValue(args, "import", (s) => {
                    var talkGroupCsvPath = (new FileInfo(s)).FullName;
                    return (new SignalRadioClient(new Uri(_liquidConfig.ConnectionString)))
                        .ImportTalkgroupCsvAsync(s)
                            .GetAwaiter()
                                .GetResult();
                });

                if(importResults is null)
                {
                    var callWavPath = args[1];
                    callWavPath = (new FileInfo(callWavPath)).FullName;

                    if(string.IsNullOrEmpty(callWavPath) || !File.Exists(callWavPath))
                        throw new Exception("Invalid callWavPath :(");
                    
                    using(var clientSocket = new UdpSocket())
                    {
                        clientSocket.Client("127.0.0.1", 27000);
                        clientSocket.Send(callWavPath);
                    }
                }
                else
                {
                    System.Console.WriteLine(importResults.ToString());
                }
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }

        private static void StartUdpServer(CancellationToken cancellationToken)
        {
            var callHandler = new CallHandler(_liquidConfig);
            using(var socket = new UdpSocket())
            {
                try
                {
                    socket.Server("127.0.0.1", 27000, async (msg) => 
                    {
                        if(string.Compare(msg, "shutdown", true) == 0)
                            _cancellationTokenSource.Cancel();
                            
                        await callHandler.HandleCallAsync(msg, null, _cancellationTokenSource.Token);  
                    });

                    while(!cancellationToken.IsCancellationRequested)
                        Task.Delay(200).Wait(cancellationToken);
                }
                finally
                {
                    _cancellationTokenSource.Cancel();
                }
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
