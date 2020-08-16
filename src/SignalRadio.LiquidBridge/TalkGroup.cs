namespace SignalRadio.LiquidBridge
{
    public class TalkGroup
    {
        public ushort Identifier {get;set;}
        public ushort Priority {get;set;}
        public string Name {get;set;}
        public string Mode { get; set; }
        public string Tag { get; set; }
        public string AlphaTag { get; set; }
        public string Description { get; set; }
        public string[] Streams { get; set; } 
    }
}
