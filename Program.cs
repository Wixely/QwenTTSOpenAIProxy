using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using QwenTtsOpenAIProxy.Models;
using QwenTtsOpenAIProxy.Options;
using QwenTtsOpenAIProxy.Services;

namespace QwenTtsOpenAIProxy;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables();
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8092));

        builder.Services
            .AddOptions<QwenBackendOptions>()
            .Bind(builder.Configuration.GetSection(QwenBackendOptions.SectionName))
            .PostConfigure(options =>
            {
                string? backendBaseUrl = builder.Configuration["QWEN_BACKEND_BASE_URL"];
                if (!string.IsNullOrWhiteSpace(backendBaseUrl))
                {
                    options.BaseUrl = backendBaseUrl;
                }

                string? defaultModel = builder.Configuration["QWEN_DEFAULT_MODEL"];
                if (!string.IsNullOrWhiteSpace(defaultModel))
                {
                    options.DefaultModel = defaultModel;
                }

                options.BaseUrl = options.BaseUrl.Trim();
                options.DefaultModel = options.DefaultModel.Trim();
                options.SpeechPath = NormalizePath(options.SpeechPath, "/v1/audio/speech");
                options.StreamSpeechPath = NormalizePath(options.StreamSpeechPath, "/v1/audio/speech/stream");
            })
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                "QwenBackend:BaseUrl or QWEN_BACKEND_BASE_URL must be an absolute http/https URL.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DefaultModel),
                "QwenBackend:DefaultModel or QWEN_DEFAULT_MODEL is required.")
            .Validate(
                options => options.TimeoutSeconds > 0,
                "QwenBackend:TimeoutSeconds must be greater than zero.")
            .ValidateOnStart();

        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter<SpeechResponseFormat>(JsonNamingPolicy.SnakeCaseLower));
            });

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddHttpClient(HttpClientNames.QwenBackend, ConfigureBackendHttpClient);
        builder.Services.AddHttpClient<QwenOmniHttpTtsBackendClient>(ConfigureBackendHttpClient);
        builder.Services.AddHttpClient<QwenOmniStreamTtsBackendClient>(ConfigureBackendHttpClient);
        builder.Services.AddTransient<ITtsBackendClient>(serviceProvider =>
            serviceProvider.GetRequiredService<QwenOmniHttpTtsBackendClient>());

        WebApplication app = builder.Build();

        app.MapControllers();

        await app.RunAsync().ConfigureAwait(false);
    }

    private static void ConfigureBackendHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        QwenBackendOptions options = serviceProvider.GetRequiredService<IOptions<QwenBackendOptions>>().Value;

        httpClient.BaseAddress = options.CreateBaseUri();
        httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }

    private static string NormalizePath(string? path, string fallback)
    {
        string normalized = string.IsNullOrWhiteSpace(path) ? fallback : path.Trim();
        return normalized.StartsWith('/') ? normalized : "/" + normalized;
    }
}

public static class HttpClientNames
{
    public const string QwenBackend = "QwenBackend";
}
