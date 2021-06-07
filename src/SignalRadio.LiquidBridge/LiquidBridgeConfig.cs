using System.IO;
using System.Text;

namespace SignalRadio.LiquidBridge
{
    public class LiquidBridgeConfig
    {
        public string IcecastHost  { get; set; }
        public int IcecastPort  { get; set; }
        public string UdpServerIpAddress { get; set; }
        public int UdpServerPort { get; set; }
        public string StreamPassword  { get; set; }
        public string LiquidsoapTemplatePath { get; set; }
        public string LiquidsoapSocketsPath { get; set; }
        public string ConnectionString { get; set; }
        
        public string BuildLiquidsoapConfig(string streamId, string streamName, string description, string genra)
        {
            var configBuilder = new StringBuilder();
            
            configBuilder.AppendLine("#!/usr/bin/liquidsoap");
            configBuilder.AppendLine(string.Format("HOST=\"{0}\"", IcecastHost));
            configBuilder.AppendLine(string.Format("PORT={0}", IcecastPort));
            configBuilder.AppendLine(string.Format("MOUNT=\"/{0}\"", streamId));
            configBuilder.AppendLine(string.Format("PASSWORD=\"{0}\"", StreamPassword));
            configBuilder.AppendLine(string.Format("NAME=\"{0}\"", streamName));
            configBuilder.AppendLine(string.Format("DESCRIPTION=\"{0}\"", description));
            configBuilder.AppendLine(string.Format("GENRA=\"{0}\"", genra));
            //This controls the socket name
            configBuilder.AppendLine(string.Format("STREAMID=\"{0}\"", streamId));
            configBuilder.Append(string.Format("%include \"{0}\"", LiquidsoapTemplatePath));
            
            var configFileName = string.Format("{0}.liq", Path.GetRandomFileName());
            var configFilePath = Path.Join(Path.GetTempPath(), configFileName);

            File.AppendAllText(configFilePath, configBuilder.ToString());
            return configFilePath;
        }
    }
}
