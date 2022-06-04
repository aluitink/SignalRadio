
using System.IO;
using System.Reflection;

namespace SignalRadio.Web.Api.Test
{
    public class TestBase
    {
        protected byte[] GetTrunkRecorderAssetsAsBytes(string fileName)
        {
            using (var s = GetEmbeddedResource("TrunkRecorderStatusMessages", fileName))
            {
                byte[] buffer = new byte[s.Length];
                s.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        protected Stream GetTalkGroupFiles(string fileName)
        {
            return GetEmbeddedResource("TalkGroupFiles", fileName);
        }

        protected Stream GetEmbeddedResource(string assetsFolder, string fileName)
        {
            var type = typeof(TrunkRecorderStatusHandlerTest);

            var assembly = typeof(TrunkRecorderStatusHandlerTest).GetTypeInfo().Assembly;
            if (assembly == null)
                throw new System.Exception("Could not find Assembly");
            var resource = string.Format("{0}.Assets.{1}.{2}", type.Namespace, assetsFolder, fileName);

            return assembly.GetManifestResourceStream(resource);
        }
    }
}