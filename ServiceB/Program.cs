using MongoDB.Driver;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ServiceB.Models;

var builder = WebApplication.CreateBuilder(args);

// Variables de entorno
var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "ServiceB";
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4318";
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "*";
var serviceAUrl = Environment.GetEnvironmentVariable("SERVICEA_URL") ?? "http://localhost:5000";
var mongoConnection = Environment.GetEnvironmentVariable("MONGO_CONNECTION") ?? "mongodb://localhost:27017";
var mongoDatabase = Environment.GetEnvironmentVariable("MONGO_DATABASE") ?? "testdb";

// Configurar OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource(serviceName)
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(otlpEndpoint);
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

// Configurar HttpClient
builder.Services.AddHttpClient();

// Configurar MongoDB
builder.Services.AddSingleton<IMongoCollection<Item>>(serviceProvider =>
{
    var client = new MongoClient(mongoConnection);
    var database = client.GetDatabase(mongoDatabase);
    return database.GetCollection<Item>("items");
});

var app = builder.Build();

// Configurar pipeline
app.UseCors();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = serviceName }));

// Endpoint que consulta ServiceA
app.MapGet("/api/values-from-a", async (HttpClient httpClient) =>
{
    try
    {
        var response = await httpClient.GetAsync($"{serviceAUrl}/api/values");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return Results.Ok(new { source = "ServiceA", data = content });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error calling ServiceA: {ex.Message}");
    }
});

// Endpoint que lee de MongoDB
app.MapGet("/api/items", async (IMongoCollection<Item> collection) =>
{
    try
    {
        var items = await collection.Find(_ => true).ToListAsync();
        return Results.Ok(items);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error accessing MongoDB: {ex.Message}");
    }
});

app.Run(); 