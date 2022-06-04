namespace SignalRadio.Public.Lib.Models
{
    public class RadioSource
    {
        public uint Id { get; set; }
        public ushort SourceNumber { get; set; }
        public string Antenna { get; set; }
        public bool IsQPSK { get; set; }
        public uint SilenceFrames { get; set; }
        public ushort AnalogLevels { get; set; }
        public ushort DigitalLevels { get; set; }
        public uint MinHz { get; set; }
        public uint MaxHz { get; set; }
        public uint CenterHz { get; set; }
        public uint Rate { get; set; }
        public string Driver { get; set; }
        public string Device { get; set; }
        public ushort Error { get; set; }
        public ushort MixGain { get; set; }
        public ushort LnaGain { get; set; }
        public ushort Vga1Gain { get; set; }
        public ushort Vga2Gain { get; set; }
        public uint BBGain { get; set; }
        public ushort Gain { get; set; }
        public ushort IfGain { get; set; }
        public ushort SquelchDB { get; set; }
        public ushort AnalogRecorders { get; set; }
        public ushort DigitalRecorders { get; set; }
        public ushort DebugRecorders { get; set; }
        public ushort SigmfRecorders { get; set; }

        public static RadioSource FromSource(TrunkRecorder.Source source)
        {
            var radioSource = new RadioSource();
            radioSource.UpdateFromSource(source);
            return radioSource;
        }

        public void UpdateFromSource(TrunkRecorder.Source source)
        {
            SourceNumber = (ushort)source.SourceNumber;
            if (!string.IsNullOrEmpty(source.Antenna))
                Antenna = source.Antenna;
            if (!string.IsNullOrEmpty(source.Qpsk))
                IsQPSK = bool.Parse(source.Qpsk);
            if (!string.IsNullOrEmpty(source.SilenceFrames))
                SilenceFrames = uint.Parse(source.SilenceFrames);
            if (!string.IsNullOrEmpty(source.AnalogLevels))
                AnalogLevels = ushort.Parse(source.AnalogLevels);
            if (!string.IsNullOrEmpty(source.DigitalLevels))
                DigitalLevels = ushort.Parse(source.DigitalLevels);
            if (!string.IsNullOrEmpty(source.MinHz))
                MinHz = uint.Parse(source.MinHz);
            if (!string.IsNullOrEmpty(source.MaxHz))
                MaxHz = uint.Parse(source.MaxHz);
            if (!string.IsNullOrEmpty(source.Center))
                CenterHz = uint.Parse(source.Center);
            if (!string.IsNullOrEmpty(source.Rate))
                Rate = uint.Parse(source.Rate);
            if (!string.IsNullOrEmpty(source.Driver))
                Driver = source.Driver;
            if (!string.IsNullOrEmpty(source.Device))
                Device = source.Device;
            if (!string.IsNullOrEmpty(source.Error))
                Error = ushort.Parse(source.Error);
            if (!string.IsNullOrEmpty(source.MixGain))
                MixGain = ushort.Parse(source.MixGain);
            if (!string.IsNullOrEmpty(source.LnaGain))
                LnaGain = ushort.Parse(source.LnaGain);
            if (!string.IsNullOrEmpty(source.Vga1Gain))
                Vga1Gain = ushort.Parse(source.Vga1Gain);
            if (!string.IsNullOrEmpty(source.Vga2Gain))
                Vga2Gain = ushort.Parse(source.Vga2Gain);
            if (!string.IsNullOrEmpty(source.BbGain))
                BBGain = uint.Parse(source.BbGain);
            if (!string.IsNullOrEmpty(source.Gain))
                Gain = ushort.Parse(source.Gain);
            if (!string.IsNullOrEmpty(source.IfGain))
                IfGain = ushort.Parse(source.IfGain);
            if (!string.IsNullOrEmpty(source.SquelchDb))
                SquelchDB = ushort.Parse(source.SquelchDb);
            if (!string.IsNullOrEmpty(source.AnalogRecorders))
                AnalogRecorders = ushort.Parse(source.AnalogRecorders);
            if (!string.IsNullOrEmpty(source.DigitalRecorders))
                DigitalRecorders = ushort.Parse(source.DigitalRecorders);
            if (!string.IsNullOrEmpty(source.DebugRecorders))
                DebugRecorders = ushort.Parse(source.DebugRecorders);
            if (!string.IsNullOrEmpty(source.SigmfRecorders))
                SigmfRecorders = ushort.Parse(source.SigmfRecorders);
        }

        public override string ToString()
        {
            return string.Format("RadioSource[{0}] - Device/Driver: {1}{5}, Min: {2}, Center: {3}, Max: {4}", SourceNumber, Device, MinHz, CenterHz, MaxHz, Driver);
        }
    }
}
