using SignalRadio.Public.Lib.Models.Enums;

using System;

namespace SignalRadio.Public.Lib.Models
{
    public class RadioRecorder
    {
        public uint Id { get; set; }
        public string RecorderIdentifier { get; set; }
        public RadioSystemType Type { get; set; }
        public ushort SourceNumber { get; set; }
        public ushort RecorderNumber { get; set; }
        public uint Count { get; set; }
        public float Duration { get; set; }
        public ushort State { get; set; }
        public uint StatusLength { get; set; }
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
            Count = (uint)recorder.Count;

            if (!string.IsNullOrWhiteSpace(recorder.SourceNumber))
                SourceNumber = ushort.Parse(recorder.SourceNumber);
            if(!string.IsNullOrWhiteSpace(recorder.State))
                State = ushort.Parse(recorder.State);
            if(!string.IsNullOrWhiteSpace(recorder.Duration))
                Duration = float.Parse(recorder.Duration);
            if(!string.IsNullOrWhiteSpace(recorder.StatusLength))
                StatusLength = uint.Parse(recorder.StatusLength);
            if(!string.IsNullOrWhiteSpace(recorder.StatusError))
                StatusError = ushort.Parse(recorder.StatusError);
            if(!string.IsNullOrWhiteSpace(recorder.StatusSpike))
                StatusSpike = ushort.Parse(recorder.StatusSpike);
            if (!string.IsNullOrWhiteSpace(recorder.Type))
                Type = (RadioSystemType)Enum.Parse(typeof(RadioSystemType), recorder.Type, true);
        }

        public override string ToString()
        {
            return string.Format("RadioRecorder[{0}]: SourceID: {1}, State: {2}, Duration: {3}", RecorderIdentifier, SourceNumber, State, Duration);
        }
    }
}
