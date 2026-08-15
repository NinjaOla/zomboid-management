using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZombieGateway.Options;

namespace ZombieGateway.Services;

public sealed class RemoteManagementClient : ISystemdServiceController, IZomboidRconClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly ManagementApiOptions _options;

    public RemoteManagementClient(HttpClient httpClient, IOptions<ManagementApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ServiceStatusResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/server/status", cancellationToken);
        return await ReadJsonOrThrowAsync<ServiceStatusResult>(response, cancellationToken);
    }

    public async Task<ServiceOperationResult> StartAsync(CancellationToken cancellationToken)
    {
        var response = await SendAsync(HttpMethod.Post, "/server/start", cancellationToken);
        return await ReadJsonOrThrowAsync<ServiceOperationResult>(response, cancellationToken);
    }

    public async Task<ServiceOperationResult> StopAsync(CancellationToken cancellationToken)
    {
        var response = await SendAsync(HttpMethod.Post, "/server/stop", cancellationToken);
        return await ReadJsonOrThrowAsync<ServiceOperationResult>(response, cancellationToken);
    }

    public async Task<string> GetPlayersAsync(CancellationToken cancellationToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/server/players", cancellationToken);
        var body = await ReadJsonOrThrowAsync<PlayersResponse>(response, cancellationToken);
        return body.Output;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("X-Api-Key", _options.ApiKey);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        return response;
    }

    private static async Task<T> ReadJsonOrThrowAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);
            if (body is null)
            {
                throw new InvalidOperationException("Management API response body was empty.");
            }

            return body;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Management API call failed with {(int)response.StatusCode}: {error}");
    }

    private sealed record PlayersResponse(string Output);
}
