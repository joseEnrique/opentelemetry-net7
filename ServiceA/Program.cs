using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Variables de entorno
var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "ServiceA";
var jaegerEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_JAEGER_ENDPOINT") ?? "http://localhost:14268/api/traces";
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "*";
var serviceBUrl = Environment.GetEnvironmentVariable("SERVICEB_URL") ?? "http://localhost:5001";

// Configurar OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource(serviceName)
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddJaegerExporter(jaegerOptions =>
            {
                jaegerOptions.Endpoint = new Uri(jaegerEndpoint);
            }));

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins == "*")
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins.Split(','))
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Agregar servicios adicionales
builder.Services.AddHttpClient();

var app = builder.Build();

// Configurar pipeline
app.UseCors();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = serviceName }));

// Endpoint principal
app.MapGet("/api/values", () =>
{
    var values = new[] { "value1", "value2", "value3", "serviceA-data" };
    return Results.Ok(values);
});

// Endpoint que consulta ServiceB
app.MapGet("/api/items-from-b", async (HttpClient httpClient) =>
{
    try
    {
        var response = await httpClient.GetAsync($"{serviceBUrl}/api/items");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return Results.Ok(new { source = "ServiceB", data = content });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error calling ServiceB: {ex.Message}");
    }
});

app.Run(); 