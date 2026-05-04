using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using QwenTtsOpenAIProxy.Models;
using QwenTtsOpenAIProxy.Options;

namespace QwenTtsOpenAIProxy.Services;

public sealed class QwenOmniHttpTtsBackendClient : ITtsBackendClient
{
    private const int ErrorBodyLimit = 4096;

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<QwenOmniHttpTtsBackendClient> _logger;
    private readonly QwenBackendOptions _options;

    public QwenOmniHttpTtsBackendClient(
        HttpClient httpClient,
        IOptions<QwenBackendOptions> options,
        ILogger<QwenOmniHttpTtsBackendClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _jsonOptions.Converters.Add(
            new JsonStringEnumConverter<SpeechResponseFormat>(JsonNamingPolicy.SnakeCaseLower));
    }

    public async Task<TtsBackendResult> CreateSpeechAsync(
        SpeechRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string route = _options.SpeechPath;
        _logger.LogInformation(
            "Forwarding TTS request to Qwen backend HTTP route {Route}. Model: {Model}; voice: {Voice}; format: {Format}; speed: {Speed}.",
            route,
            request.Model,
            request.Voice,
            request.ResponseFormat,
            request.Speed);

        using JsonContent requestContent = JsonContent.Create(request, options: _jsonOptions);
        HttpResponseMessage backendResponse;

        try
        {
            backendResponse = await _httpClient.PostAsync(route, requestContent, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timed out calling Qwen backend route {Route}.", route);
            return TtsBackendResult.Error(
                StatusCodes.Status502BadGateway,
                $"Timed out calling Qwen backend POST {BuildBackendUrl(route)}.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach Qwen backend route {Route}.", route);
            return TtsBackendResult.Error(
                StatusCodes.Status502BadGateway,
                $"Could not reach Qwen backend POST {BuildBackendUrl(route)}: {ex.Message}");
        }

        using (backendResponse)
        {
            if (backendResponse.StatusCode == HttpStatusCode.NotFound)
            {
                string? backendBody = await ReadErrorPreviewAsync(backendResponse.Content, cancellationToken)
                    .ConfigureAwait(false);
                string message =
                    $"Qwen backend returned 404 for POST {BuildBackendUrl(route)}. vLLM-Omni did not expose the OpenAI-compatible /v1/audio/speech route. The proxy did not generate fake audio; implement or select the stream adapter for {_options.StreamSpeechPath}.";

                if (!string.IsNullOrWhiteSpace(backendBody))
                {
                    message += $" Backend response: {backendBody}";
                }

                _logger.LogWarning("{Message}", message);
                return TtsBackendResult.Error(StatusCodes.Status502BadGateway, message);
            }

            if (!backendResponse.IsSuccessStatusCode)
            {
                string? backendBody = await ReadErrorPreviewAsync(backendResponse.Content, cancellationToken)
                    .ConfigureAwait(false);
                string message =
                    $"Qwen backend POST {BuildBackendUrl(route)} returned {(int)backendResponse.StatusCode} {backendResponse.ReasonPhrase}.";

                if (!string.IsNullOrWhiteSpace(backendBody))
                {
                    message += $" Backend response: {backendBody}";
                }

                _logger.LogWarning("{Message}", message);
                return TtsBackendResult.Error(StatusCodes.Status502BadGateway, message);
            }

            byte[] audioBytes = await backendResponse.Content.ReadAsByteArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            if (audioBytes.Length == 0)
            {
                string message =
                    $"Qwen backend POST {BuildBackendUrl(route)} returned success but the audio payload was empty.";
                _logger.LogWarning("{Message}", message);
                return TtsBackendResult.Error(StatusCodes.Status502BadGateway, message);
            }

            SpeechResponseFormat responseFormat = request.ResponseFormat ?? SpeechResponseFormat.Wav;
            string contentType = responseFormat.ToContentType();

            _logger.LogInformation(
                "Received {ByteCount} bytes of {ContentType} audio from Qwen backend.",
                audioBytes.Length,
                contentType);

            return TtsBackendResult.Audio(audioBytes, contentType);
        }
    }

    private async Task<string?> ReadErrorPreviewAsync(HttpContent content, CancellationToken cancellationToken)
    {
        try
        {
            string body = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            return body.Length <= ErrorBodyLimit
                ? body
                : body[..ErrorBodyLimit] + "...";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read backend error response body.");
            return null;
        }
    }

    private string BuildBackendUrl(string route)
    {
        return new Uri(_options.CreateBaseUri(), route.TrimStart('/')).ToString();
    }
}
