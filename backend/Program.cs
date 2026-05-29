using SapAnalytics.Application;
using SapAnalytics.Application.Ports;
using SapAnalytics.Infrastructure.Outbound.MockTxt;
using SapAnalytics.Infrastructure.Outbound.Sap;
using SapAnalytics.Infrastructure.Outbound.Sqlite;

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
    });
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
// first load. Skipped under the "Testing" environment (integration tests wire their own store).
// An expected failure (source unavailable) is ignored on purpose: POST /api/sales/refresh retries.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var ingest = scope.ServiceProvider.GetRequiredService<IngestSales>();
    await ingest.ExecuteAsync();
}

app.Run();

// Exposed as a public partial class so the test project can reference Program
// for WebApplicationFactory<Program> integration tests.
public partial class Program;
