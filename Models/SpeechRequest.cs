using System.Text.Json.Serialization;

namespace QwenTtsOpenAIProxy.Models;

public sealed class SpeechRequest
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("voice")]
    public string? Voice { get; set; }

    [JsonPropertyName("response_format")]
    public SpeechResponseFormat? ResponseFormat { get; set; }

    [JsonPropertyName("speed")]
    public double? Speed { get; set; }

    public SpeechRequest Normalize(string defaultModel)
    {
        return new SpeechRequest
        {
            Model = string.IsNullOrWhiteSpace(Model) ? defaultModel : Model.Trim(),
            Input = Input,
            Voice = string.IsNullOrWhiteSpace(Voice) ? Voice : Voice.Trim(),
            ResponseFormat = ResponseFormat ?? SpeechResponseFormat.Wav,
            Speed = Speed
        };
    }
}
