using MessageBroker.Data;
using MessageBroker.Models;
using Microsoft.EntityFrameworkCore;

// Create web app builder
var builder = WebApplication.CreateBuilder(args);

// Configures Entity Framework with SQLite to connect to MessageBroker.db in data source
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=MessageBroker.db"));

var app = builder.Build();

app.UseHttpsRedirection();

// Create a Topic at /api/topics (POST)
// AppDbContext is injected automatically with DI and request body is deserialized
// to Topic object
app.MapPost("api/topics", async (AppDbContext context, Topic topic) =>
{
    await context.Topics.AddAsync(topic); // Adds topic to the Topics DbSet
    await context.SaveChangesAsync(); // Saves changes to SQLite database

    // Returns a 201 status with location header of api/topics/id 
    // and response body of Topic object
    return Results.Created($"api/topics/{topic.Id}", topic);
});

// Return all topics at api/topics (GET)
app.MapGet("api/topics", async (AppDbContext context) => {
    // Queries the database to get all Topic records and converts it to a list
    var topics = await context.Topics.ToListAsync();

    return Results.Ok(topics); // Returns 200 status with topics list as JSON in response body
});

app.Run();


