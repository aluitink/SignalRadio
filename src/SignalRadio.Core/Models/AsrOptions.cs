namespace SignalRadio.Core.Models;

public class AsrOptions
{
    public const string SectionName = "AsrSettings";
    
    public bool Enabled { get; set; } = false;
    public string WhisperServiceUrl { get; set; } = "http://whisper-asr:9000";
    public bool AutoTranscribe { get; set; } = true;
    public string OutputFormat { get; set; } = "json";
    public bool WordTimestamps { get; set; } = false;
    public bool VadFilter { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 300;
    
    /// <summary>
    /// The full URL for the ASR transcription endpoint
    /// </summary>
    public string TranscriptionEndpoint => $"{WhisperServiceUrl.TrimEnd('/')}/asr";
    
    /// <summary>
    /// The full URL for the language detection endpoint
    /// </summary>
    public string LanguageDetectionEndpoint => $"{WhisperServiceUrl.TrimEnd('/')}/detect-language";
}
