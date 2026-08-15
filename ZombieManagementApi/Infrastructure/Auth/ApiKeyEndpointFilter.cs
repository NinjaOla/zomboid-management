using Microsoft.Extensions.Options;
using ZombieManagementApi.Infrastructure.Configuration;

namespace ZombieManagementApi.Infrastructure.Auth;

public sealed class ApiKeyEndpointFilter : IEndpointFilter
{
    private const string HeaderName = "X-Api-Key";
    private readonly ManagementAuthOptions _options;
    private readonly ILogger<ApiKeyEndpointFilter> _logger;

    public ApiKeyEndpointFilter(IOptions<ManagementAuthOptions> options, ILogger<ApiKeyEndpointFilter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("ManagementAuth:ApiKey is not configured.");
            return ValueTask.FromResult<object?>(Results.Problem("Management API key is not configured.", statusCode: 503));
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey))
            return ValueTask.FromResult<object?>(Results.Unauthorized());

        if (!string.Equals(providedKey.ToString(), _options.ApiKey, StringComparison.Ordinal))
            return ValueTask.FromResult<object?>(Results.Unauthorized());

        return next(context);
    }
}
