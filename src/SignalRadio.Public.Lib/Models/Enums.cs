namespace SignalRadio.Public.Lib.Models.Enums
{
    public enum RadioSystemType: byte
    {
        None,
        P25Phase1,
        P25Phase2
    }


    public enum RadioSystemVoice
    {
        None,
        APCO25
    }


    public enum TalkGroupMode: byte
    {
        None,
        Digital,
        DigitalEncrypted,
        Analog,
        Test
    }

    public enum TalkGroupTag : byte
    {
        None,
        Interop,
        PublicWorks,
        Hospital,
        Schools,
        
        FireTalk,
        LawTalk,
        MultiTalk,

        FireTac,
        LawTac,
        MultiTac,
        
        EMSDispatch,
        LawDispatch,
        MultiDispatch,

        EmergencyOps
    }
}