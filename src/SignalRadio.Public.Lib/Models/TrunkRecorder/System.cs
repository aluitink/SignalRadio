using System.Collections.Generic;
using Newtonsoft.Json;


namespace SignalRadio.Public.Lib.Models.TrunkRecorder
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

        [JsonProperty("sysid")]
        public int SystemId { get; set; }

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

        [JsonProperty("nac")]
        public int NAC { get;set; }
        
        [JsonProperty("wacn")]
        public int WACN { get;set; }

        //[JsonProperty("Id")]
        //private int _Id { set { SystemNumber = value; } }

        [JsonProperty("name")]
        private string _SystemName { set { ShortName = value; } }

        [JsonProperty("type")]
        private string _Type { set { SystemType = value; } }
    }
}
