using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConnectAnalytics.Domain;

namespace ConnectAnalytics.Infrastructure.Outbound.Shopify;

// Obtains and caches the Admin API access token from Shopify's Client Credentials Grant
// (POST /admin/oauth/access_token). Apps created in the Dev Dashboard don't expose a static
// shpat_ token in the UI anymore: the token is fetched programmatically from client_id +
// client_secret. The response token is "offline" (no expires_in), so we keep it in memory
// for the process lifetime and lazy-fetch on first use.
//
// Concurrency: the first caller wins the SemaphoreSlim and performs the HTTP call; everyone
// else awaits and reads the cached value, so we never hit the OAuth endpoint twice in parallel.
//
// TODO: react to a future 401 from the data endpoint by invalidating the cached token and
// retrying once (Shopify rotates/revokes tokens out-of-band).
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
                // A 2xx response with a body that is not the JSON shape we expect is unexpected,
                // not Validation: the caller's request was perfectly well-formed.
                return Result<string>.Failure(Error.Unexpected(
                    $"Malformed payload from Shopify's OAuth endpoint: {ex.Message}"));
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // HttpClient throws TaskCanceledException (an OCE) on its internal Timeout even
                // when the caller's token hasn't been cancelled. Treat as the source being
                // unavailable, not as a business cancellation.
                return Result<string>.Failure(Error.Unavailable(
                    "Shopify OAuth request timed out."));
            }
        }
        finally
        {
            _gate.Release();
        }
        // OperationCanceledException intentionally propagates when the caller cancelled.
    }

    private sealed record TokenRequest(
        [property: JsonPropertyName("client_id")] string ClientId,
        [property: JsonPropertyName("client_secret")] string ClientSecret,
        [property: JsonPropertyName("grant_type")] string GrantType);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("scope")] string? Scope);
}
