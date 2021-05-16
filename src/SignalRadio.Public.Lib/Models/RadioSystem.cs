using System.Text.Json.Serialization;
using SignalRadio.Public.Lib.Models.Enums;
using System;
using System.Collections.ObjectModel;

namespace SignalRadio.Public.Lib.Models
{
    public class RadioSystem 
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get;set; }
        public string City { get; set; }
        public string State { get; set; }
        public string County { get; set; }
        public int NAC { get; set; }
        public int WANC { get; set; }
        public int SystemNumber { get; set; }
        public RadioSystemType SystemType { get; set; }
        public RadioSystemVoice SystemVoice { get; set; }
        public DateTime LastUpdatedUtc { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<RadioFrequency> ControlFrequencies { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<RadioGroup> RadioGroups { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Collection<TalkGroup> TalkGroups { get; set; }

        public static RadioSystem FromSystem(TrunkRecorder.System system)
        {
            var radio = new RadioSystem();
            radio.UpdateFromSystem(system);
            return radio;
        }

        public void UpdateFromSystem(TrunkRecorder.System system)
        {
            ShortName = system.ShortName;
            
            SystemNumber = system.SystemNumber;
            NAC = system.NAC;
            WANC = system.WACN;

            if(system.SystemType != null)
                SystemType = (RadioSystemType)Enum.Parse(typeof(RadioSystemType), system.SystemType, true);
            
            LastUpdatedUtc = DateTime.UtcNow;
        }
    }
}
