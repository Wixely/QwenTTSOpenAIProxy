namespace QwenTtsOpenAIProxy.Options;

public sealed class QwenBackendOptions
{
    public const string SectionName = "QwenBackend";

    public string BaseUrl { get; set; } = "http://qwen-tts:8091";

    public string DefaultModel { get; set; } = "Qwen/Qwen3-TTS-12Hz-0.6B-Base";

    public string SpeechPath { get; set; } = "/v1/audio/speech";

    public string StreamSpeechPath { get; set; } = "/v1/audio/speech/stream";

    public int TimeoutSeconds { get; set; } = 300;

    public Uri CreateBaseUri()
    {
        return new Uri(BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }
}
