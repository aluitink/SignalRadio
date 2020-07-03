using System.Collections.Generic;
using Newtonsoft.Json;


namespace SignalRadio.Web.Api.Models.TrunkRecorder
{
    public class System
    {
        [JsonProperty("audioArchive")]
        public string AudioArchive { get; set; }

        [JsonProperty("systemType")]
        public string SystemType { get; set; }

        [JsonProperty("shortName")]
        public string ShortName { get; set; }

        [JsonProperty("sysNum")]
        public int SystemNumber { get; set; }

        [JsonProperty("uploadScript")]
        public string UploadScript { get; set; }

        [JsonProperty("recordUnkown")]
        public string RecordUnkown { get; set; }

        [JsonProperty("callLog")]
        public string CallLog { get; set; }

        [JsonProperty("talkgroupsFile")]
        public string TalkgroupsFile { get; set; }

        [JsonProperty("channels")]
        public IList<string> Channels { get; set; }
    }
}
