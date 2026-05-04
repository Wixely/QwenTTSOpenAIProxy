using Microsoft.AspNetCore.Mvc;

namespace QwenTtsOpenAIProxy.Controllers;

[ApiController]
[Route("v1/models")]
public sealed class ModelsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(
        IHttpClientFactory httpClientFactory,
        ILogger<ModelsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetModels(CancellationToken cancellationToken)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientNames.QwenBackend);

        _logger.LogInformation("Proxying model discovery request to Qwen backend /v1/models.");

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/v1/models");
            using HttpResponseMessage response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            Response.StatusCode = (int)response.StatusCode;
            Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

            await response.Content.CopyToAsync(Response.Body, cancellationToken).ConfigureAwait(false);
            return new EmptyResult();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timed out calling Qwen backend /v1/models.");
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    error = new
                    {
                        type = "bad_gateway",
                        message = "Timed out calling Qwen backend GET /v1/models."
                    }
                });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach Qwen backend /v1/models.");
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    error = new
                    {
                        type = "bad_gateway",
                        message = $"Could not reach Qwen backend GET /v1/models: {ex.Message}"
                    }
                });
        }
    }
}
