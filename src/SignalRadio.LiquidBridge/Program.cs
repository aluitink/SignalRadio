using System;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
            
            if (isHelpRequested)
            {
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
                    //config.TalkGroupCsvPath = (new FileInfo(config.TalkGroupCsvPath)).FullName;

                    return config;
                });

                var handler = new CallHandler(_liquidConfig);

                if(IsArgumentFlagExists(args, "import"))
                {
                    var client = new SignalRadioClient(new Uri(_liquidConfig.ConnectionString));
                    var results = client.ImportTalkgroupCsvAsync(_liquidConfig.TalkGroupCsvPath).Result;
                    System.Console.WriteLine("Imported Results: {0}", results.ToString());
                }
                else
                {
                    var callWavPath = args[1];

                    if(string.IsNullOrEmpty(callWavPath) || !File.Exists(callWavPath))
                        throw new Exception("Invalid callWavPath :(");

                    handler.HandleCallAsync(callWavPath).GetAwaiter().GetResult();
                }                
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e.ToString());
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
