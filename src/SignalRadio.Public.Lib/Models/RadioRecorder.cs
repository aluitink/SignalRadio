namespace SignalRadio.Public.Lib.Models
{
    public class RadioRecorder
    {
        public uint Id { get; set; }
        public string RecorderIdentifier { get; set; }
        public string Type { get; set; }
        public ushort SourceNumber { get; set; }
        public ushort RecorderNumber { get; set; }
        public uint Count { get; set; }
        public float Duration { get; set; }
        public ushort State { get; set; }
        public ushort StatusLength { get; set; }
        public ushort StatusError { get; set; }
        public ushort StatusSpike { get; set; }

        public static RadioRecorder FromRecorder(TrunkRecorder.Recorder recorder)
        {
            var radioRecorder = new RadioRecorder();
            radioRecorder.UpdateFromRecorder(recorder);
            return radioRecorder;
        }

        public void UpdateFromRecorder(TrunkRecorder.Recorder recorder)
        {
            RecorderIdentifier = recorder.Id;
            RecorderNumber = (ushort)recorder.RecorderNumber;
            SourceNumber = ushort.Parse(recorder.SourceNumber);
            State = ushort.Parse(recorder.State);
            Type = recorder.Type;
            Count = (uint)recorder.Count;
            Duration = float.Parse(recorder.Duration);
            StatusLength = ushort.Parse(recorder.StatusLength);
            StatusError = ushort.Parse(recorder.StatusError);
            StatusSpike = ushort.Parse(recorder.StatusSpike);
        }
    }
}
