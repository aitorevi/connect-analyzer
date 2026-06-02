using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConnectAnalyzer.Domain;

namespace ConnectAnalyzer.Infrastructure.Outbound.Shopify;

public sealed class ShopifyTokenProvider(HttpClient http, string clientId, string clientSecret)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _cachedToken;

    public async Task<Result<string>> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cachedToken is not null)
            return Result<string>.Success(_cachedToken);

        await _gate.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null)
                return Result<string>.Success(_cachedToken);

            var request = new TokenRequest(clientId, clientSecret, "client_credentials");

            try
            {
                using var response = await http.PostAsJsonAsync("/admin/oauth/access_token", request, ct);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    return Result<string>.Failure(Error.Unauthorized(
                        "Shopify rejected the client credentials (check Shopify:ClientId / Shopify:ClientSecret and that the app is installed)."));

                if (!response.IsSuccessStatusCode)
                    return Result<string>.Failure(Error.Unavailable(
                        $"Shopify OAuth endpoint returned {(int)response.StatusCode}."));

                var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
                if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
                    return Result<string>.Failure(Error.Unexpected(
                        "Shopify OAuth response did not contain an access_token."));

                _cachedToken = payload.AccessToken;
                return Result<string>.Success(_cachedToken);
            }
            catch (HttpRequestException ex)
            {
                return Result<string>.Failure(Error.Unavailable(
                    $"Could not reach Shopify's OAuth endpoint: {ex.Message}"));
            }
            catch (JsonException ex)
            {
                return Result<string>.Failure(Error.Unexpected(
                    $"Malformed payload from Shopify's OAuth endpoint: {ex.Message}"));
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return Result<string>.Failure(Error.Unavailable(
                    "Shopify OAuth request timed out."));
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed record TokenRequest(
        [property: JsonPropertyName("client_id")] string ClientId,
        [property: JsonPropertyName("client_secret")] string ClientSecret,
        [property: JsonPropertyName("grant_type")] string GrantType);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("scope")] string? Scope);
}
