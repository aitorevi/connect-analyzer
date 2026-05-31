using ConnectAnalyzer.Application;
using ConnectAnalyzer.Application.Ports;
using ConnectAnalyzer.Infrastructure.Outbound.MockTxt;
using ConnectAnalyzer.Infrastructure.Outbound.Sap;
using ConnectAnalyzer.Infrastructure.Outbound.Shopify;
using ConnectAnalyzer.Infrastructure.Outbound.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<SalesAnalytics>();
builder.Services.AddScoped<IngestSales>();

// Local store of sales (SQLite file). Ingestion writes here; analytics read from here. Path is
// configurable (Sqlite:Path) so a deployment can point it at a mounted volume.
var sqlitePath = builder.Configuration["Sqlite:Path"] ?? "sales.db";
builder.Services.AddSingleton<ISalesStore>(_ => new SqliteSalesStore($"Data Source={sqlitePath}"));

// Origins allowed to call the API from the browser. Defaults to the local
// frontend; override via Cors:AllowedOrigins per environment when deploying.
// Never widen this to AllowAnyOrigin.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Which data source backs ISalesRepository, chosen by configuration. Both adapters implement
// the same port: switching the origin (mock <-> real SAP) touches only this composition root.
var salesSource = builder.Configuration["SalesSource"] ?? "Mock";

if (string.Equals(salesSource, "Sap", StringComparison.OrdinalIgnoreCase))
{
    // Real SAP S/4HANA OData (Business Accelerator Hub sandbox). The API key is a secret: it
    // comes from configuration (Sap:ApiKey) and is sent as the APIKey header here, so the
    // adapter never knows it. Set it via env Sap__ApiKey or `dotnet user-secrets`.
    var sapBaseUrl = builder.Configuration["Sap:BaseUrl"]
        ?? "https://sandbox.api.sap.com/s4hanacloud/sap/opu/odata/sap/API_SALES_ORDER_SRV/";
    var sapApiKey = builder.Configuration["Sap:ApiKey"]
        ?? throw new InvalidOperationException(
            "SalesSource=Sap requires Sap:ApiKey (set it via env Sap__ApiKey or user-secrets).");
    builder.Services.AddHttpClient<ISalesRepository, SapODataSalesRepository>(client =>
    {
        client.BaseAddress = new Uri(sapBaseUrl);
        client.DefaultRequestHeaders.Add("APIKey", sapApiKey);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // The SAP sandbox gzips OData responses unconditionally; without this,
        // GetStringAsync hands the adapter the raw gzip bytes (0x1F 0x8B...) and
        // the JSON parser blows up with "0x1F is an invalid start of a value".
        AutomaticDecompression = System.Net.DecompressionMethods.All,
    });
}
else if (string.Equals(salesSource, "Shopify", StringComparison.OrdinalIgnoreCase))
{
    // Real Shopify store via Admin REST API. The Dev Dashboard no longer exposes a static
    // shpat_ token: ShopifyTokenProvider exchanges client_id + client_secret for the access
    // token at runtime via Client Credentials Grant. Both secrets stay in configuration
    // (env Shopify__ClientId / Shopify__ClientSecret); they enter the adapter through the
    // composition root, never via the domain.
    var shopifyStoreUrl = builder.Configuration["Shopify:StoreUrl"]
        ?? throw new InvalidOperationException(
            "SalesSource=Shopify requires Shopify:StoreUrl (set it via env Shopify__StoreUrl).");
    var shopifyClientId = builder.Configuration["Shopify:ClientId"]
        ?? throw new InvalidOperationException(
            "SalesSource=Shopify requires Shopify:ClientId (set it via env Shopify__ClientId or user-secrets).");
    var shopifyClientSecret = builder.Configuration["Shopify:ClientSecret"]
        ?? throw new InvalidOperationException(
            "SalesSource=Shopify requires Shopify:ClientSecret (set it via env Shopify__ClientSecret or user-secrets).");
    var shopifyApiVersion = builder.Configuration["Shopify:ApiVersion"] ?? "2025-01";

    // Single named HttpClient shared by token provider and orders repository: both speak to
    // the same store, the only difference is the path and headers per request.
    builder.Services.AddHttpClient("shopify", client =>
    {
        client.BaseAddress = new Uri(shopifyStoreUrl);
    });

    // Singleton so the in-memory access-token cache survives across requests.
    builder.Services.AddSingleton(sp =>
        new ShopifyTokenProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("shopify"),
            shopifyClientId,
            shopifyClientSecret));

    builder.Services.AddScoped<ISalesRepository>(sp =>
        new ShopifyOrdersRepository(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("shopify"),
            sp.GetRequiredService<ShopifyTokenProvider>(),
            shopifyApiVersion));
}
else
{
    // Simulated SAP export served by the local mock (default, no secrets needed).
    var sapMockBaseUrl = builder.Configuration["SapMock:BaseUrl"] ?? "http://sap-mock:8080";
    builder.Services.AddHttpClient<ISalesRepository, MockTxtSalesRepository>(client =>
    {
        client.BaseAddress = new Uri(sapMockBaseUrl);
    });
}

var app = builder.Build();

app.UseCors("AllowFrontend");
app.MapControllers();

// Seed the local store from the configured source on startup so the dashboard isn't empty on
// first load. Runs in the background with retries: on free-tier hosting the upstream source can
// take 30-60 s to wake up from a cold start, longer than the HttpClient default, so the first
// attempt frequently lands while the source is still returning 502/timeouts. Retries with backoff
// let the store self-heal without blocking app.Run(). Skipped under "Testing" (integration tests
// wire their own store). POST /api/sales/refresh remains available as a manual retry.
if (!app.Environment.IsEnvironment("Testing"))
{
    _ = Task.Run(async () =>
    {
        TimeSpan[] backoffs =
        [
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
        ];

        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        for (var attempt = 0; attempt < backoffs.Length; attempt++)
        {
            if (backoffs[attempt] > TimeSpan.Zero)
                await Task.Delay(backoffs[attempt]);

            using var scope = app.Services.CreateScope();
            var ingest = scope.ServiceProvider.GetRequiredService<IngestSales>();
            var result = await ingest.ExecuteAsync();

            var succeeded = result.Match(
                count =>
                {
                    logger.LogInformation("Startup seed ingested {Count} sales on attempt {Attempt}", count, attempt + 1);
                    return true;
                },
                error =>
                {
                    logger.LogWarning("Startup seed attempt {Attempt} failed: {Type} {Message}", attempt + 1, error.Type, error.Message);
                    return false;
                });

            if (succeeded)
                return;
        }

        logger.LogError("Startup seed gave up after {Attempts} attempts; use POST /api/sales/refresh to retry", backoffs.Length);
    });
}

app.Run();

// Exposed as a public partial class so the test project can reference Program
// for WebApplicationFactory<Program> integration tests.
public partial class Program;
