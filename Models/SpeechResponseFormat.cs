namespace QwenTtsOpenAIProxy.Models;

public enum SpeechResponseFormat
{
    Wav,
    Mp3,
    Opus,
    Flac
}

public static class SpeechResponseFormatExtensions
{
    public static string ToContentType(this SpeechResponseFormat responseFormat)
    {
        return responseFormat switch
        {
            SpeechResponseFormat.Wav => "audio/wav",
            SpeechResponseFormat.Mp3 => "audio/mpeg",
            SpeechResponseFormat.Opus => "audio/opus",
            SpeechResponseFormat.Flac => "audio/flac",
            _ => "audio/wav"
        };
    }

    public static string ToWireValue(this SpeechResponseFormat responseFormat)
    {
        return responseFormat switch
        {
            SpeechResponseFormat.Wav => "wav",
            SpeechResponseFormat.Mp3 => "mp3",
            SpeechResponseFormat.Opus => "opus",
            SpeechResponseFormat.Flac => "flac",
            _ => "wav"
        };
    }
}
