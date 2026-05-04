using QwenTtsOpenAIProxy.Models;

namespace QwenTtsOpenAIProxy.Services;

public interface ITtsBackendClient
{
    Task<TtsBackendResult> CreateSpeechAsync(SpeechRequest request, CancellationToken cancellationToken);
}

public sealed class TtsBackendResult
{
    private TtsBackendResult(
        bool success,
        byte[]? audioBytes,
        string? contentType,
        int statusCode,
        string? errorMessage)
    {
        Success = success;
        AudioBytes = audioBytes;
        ContentType = contentType;
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public byte[]? AudioBytes { get; }

    public string? ContentType { get; }

    public int StatusCode { get; }

    public string? ErrorMessage { get; }

    public static TtsBackendResult Audio(byte[] audioBytes, string contentType)
    {
        return new TtsBackendResult(true, audioBytes, contentType, StatusCodes.Status200OK, null);
    }

    public static TtsBackendResult Error(int statusCode, string message)
    {
        return new TtsBackendResult(false, null, null, statusCode, message);
    }
}
