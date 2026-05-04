using Microsoft.Extensions.Options;
using QwenTtsOpenAIProxy.Models;
using QwenTtsOpenAIProxy.Options;

namespace QwenTtsOpenAIProxy.Services;

public sealed class QwenOmniStreamTtsBackendClient : ITtsBackendClient
{
    private readonly ILogger<QwenOmniStreamTtsBackendClient> _logger;
    private readonly QwenBackendOptions _options;

    public QwenOmniStreamTtsBackendClient(
        HttpClient httpClient,
        IOptions<QwenBackendOptions> options,
        ILogger<QwenOmniStreamTtsBackendClient> logger)
    {
        _ = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task<TtsBackendResult> CreateSpeechAsync(
        SpeechRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string streamUrl = new Uri(_options.CreateBaseUri(), _options.StreamSpeechPath.TrimStart('/')).ToString();
        _logger.LogWarning(
            "Qwen stream TTS adapter selected but not implemented. Expected backend route: {Route}.",
            streamUrl);

        return Task.FromResult(TtsBackendResult.Error(
            StatusCodes.Status501NotImplemented,
            $"The Qwen stream adapter for POST {streamUrl} is a placeholder. vLLM-Omni may require HTTP streaming or WebSocket semantics for {_options.StreamSpeechPath}; implement this adapter before selecting it. No audio was generated."));
    }
}
