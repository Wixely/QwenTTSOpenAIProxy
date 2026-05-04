using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QwenTtsOpenAIProxy.Models;
using QwenTtsOpenAIProxy.Options;
using QwenTtsOpenAIProxy.Services;

namespace QwenTtsOpenAIProxy.Controllers;

[ApiController]
[Route("v1/audio")]
public sealed class AudioController : ControllerBase
{
    private readonly ILogger<AudioController> _logger;
    private readonly QwenBackendOptions _options;
    private readonly ITtsBackendClient _ttsBackendClient;

    public AudioController(
        ITtsBackendClient ttsBackendClient,
        IOptions<QwenBackendOptions> options,
        ILogger<AudioController> logger)
    {
        _ttsBackendClient = ttsBackendClient;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost("speech")]
    [Consumes("application/json")]
    [Produces("audio/wav", "audio/mpeg", "audio/opus", "audio/flac", "application/json")]
    public async Task<IActionResult> CreateSpeech(
        [FromBody] SpeechRequest? request,
        CancellationToken cancellationToken)
    {
        List<string> validationErrors = ValidateSpeechRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(CreateError(
                "invalid_request_error",
                "Invalid speech request.",
                validationErrors));
        }

        SpeechRequest normalizedRequest = request!.Normalize(_options.DefaultModel);

        _logger.LogInformation(
            "Received OpenAI-compatible TTS request. Model: {Model}; voice: {Voice}; format: {Format}; speed: {Speed}; input length: {InputLength}.",
            normalizedRequest.Model,
            normalizedRequest.Voice,
            normalizedRequest.ResponseFormat,
            normalizedRequest.Speed,
            normalizedRequest.Input?.Length ?? 0);

        TtsBackendResult backendResult = await _ttsBackendClient
            .CreateSpeechAsync(normalizedRequest, cancellationToken)
            .ConfigureAwait(false);

        if (backendResult.Success)
        {
            Response.Headers.CacheControl = "no-store";
            return File(backendResult.AudioBytes!, backendResult.ContentType!);
        }

        _logger.LogWarning(
            "TTS backend failed with proxy status {StatusCode}: {Message}",
            backendResult.StatusCode,
            backendResult.ErrorMessage);

        return StatusCode(
            backendResult.StatusCode,
            CreateError(GetErrorType(backendResult.StatusCode), backendResult.ErrorMessage ?? "TTS backend request failed."));
    }

    private static List<string> ValidateSpeechRequest(SpeechRequest? request)
    {
        List<string> errors = [];

        if (request is null)
        {
            errors.Add("Request body is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Input))
        {
            errors.Add("The `input` field is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Voice))
        {
            errors.Add("The `voice` field is required.");
        }

        if (request.Speed is not null
            && (!double.IsFinite(request.Speed.Value) || request.Speed.Value <= 0))
        {
            errors.Add("The `speed` field must be a finite number greater than zero.");
        }

        return errors;
    }

    private static object CreateError(
        string type,
        string message,
        IReadOnlyCollection<string>? details = null)
    {
        return details is null || details.Count == 0
            ? new { error = new { type, message } }
            : new { error = new { type, message, details } };
    }

    private static string GetErrorType(int statusCode)
    {
        return statusCode == StatusCodes.Status501NotImplemented
            ? "not_implemented"
            : "bad_gateway";
    }
}
