using SapAnalytics.Application.Ports;
using SapAnalytics.Infrastructure.Outbound.MockTxt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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

var sapMockBaseUrl = builder.Configuration["SapMock:BaseUrl"] ?? "http://sap-mock:8080";
builder.Services.AddHttpClient<ISalesRepository, MockTxtSalesRepository>(client =>
{
    client.BaseAddress = new Uri(sapMockBaseUrl);
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();

// Exposed as a public partial class so the test project can reference Program
// for WebApplicationFactory<Program> integration tests.
public partial class Program;
