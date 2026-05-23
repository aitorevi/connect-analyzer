using SapAnalytics.Application.Ports;
using SapAnalytics.Infrastructure.Outbound.MockTxt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var sapMockBaseUrl = builder.Configuration["SapMock:BaseUrl"] ?? "http://sap-mock";
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
