using MongoDB.Driver;
using ServiceB.Models;

var builder = WebApplication.CreateBuilder(args);

// Variables de entorno
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "*";
var serviceAUrl = Environment.GetEnvironmentVariable("SERVICEA_URL") ?? "http://localhost:5000";
var mongoConnection = Environment.GetEnvironmentVariable("MONGO_CONNECTION") ?? "mongodb://localhost:27017";
var mongoDatabase = Environment.GetEnvironmentVariable("MONGO_DATABASE") ?? "testdb";

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

// Seed test data on startup
await SeedTestData(app.Services);

// Configurar pipeline
app.UseCors();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ServiceB" }));

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

async Task SeedTestData(IServiceProvider services)
{
    try
    {
        var collection = services.GetRequiredService<IMongoCollection<Item>>();
        
        // Delete all existing items
        await collection.DeleteManyAsync(_ => true);
        
        // Insert test data
        var testItems = new List<Item>
        {
            new Item { Name = "Test Item 1" },
            new Item { Name = "Test Item 2" },
            new Item { Name = "Test Item 3" },
            new Item { Name = "Sample Product A" },
            new Item { Name = "Sample Product B" },
        };
        
        await collection.InsertManyAsync(testItems);
        
        Console.WriteLine($"Successfully seeded {testItems.Count} test items to MongoDB");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding test data: {ex.Message}");
        // Continue startup even if seeding fails
    }
} 