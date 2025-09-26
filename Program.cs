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
app.MapGet("api/topics", async (AppDbContext context) =>
{
    // Queries the database to get all Topic records and converts it to a list
    var topics = await context.Topics.ToListAsync();

    return Results.Ok(topics); // Returns 200 status with topics list as JSON in response body
});

// Publish Message (takes one incoming message and duplicates to for every subscriber to that topic)
app.MapPost("api/topics/{id}/messages", async(AppDbContext context, int id, Message message) => // Message data from request body (JSON deserialized)
{
    bool topics = await context.Topics.AnyAsync(t => t.Id == id); // Checks if topic exists by it's Id
    if (!topics) return Results.NotFound("Topic not found");
    var subs = context.Subscriptions.Where(s => s.TopicId == id); // Get all subscriptions for this topic

    // Ensure there are subscriptions (no point publishing to empty topic)
    if (subs.Count() == 0) return Results.NotFound("There are no subscriptions for this topic");

    // For each subscription, create a separate message copy
    // Each message gets the subscription ID to know where it belongs
    foreach (var sub in subs)
    {
        Message msg = new Message
        {
            TopicMessage = message.TopicMessage,
            SubscriptionId = sub.Id,
            ExpiresAfter = message.ExpiresAfter,
            MessageStatus = message.MessageStatus
        };
        // Add each message to the database
        await context.Messages.AddAsync(msg);
    }
    // Save all changes to database
    await context.SaveChangesAsync();
    return Results.Ok("Messages has been published");
});

// Create Subscription
app.MapPost("api/topics/{id}/subscriptions", async (AppDbContext context, int id, Subscription sub) =>
{
    // Checks if the topic exists using AnyAsync so a subscription towards the topic can be added
    bool topics = await context.Topics.AnyAsync(t => t.Id == id);
    if (!topics) return Results.NotFound("Topic not found");

    sub.TopicId = id; // Links subscription to the topic

    // Inserts subscription record into Subscriptions table and writes to the database
    await context.Subscriptions.AddAsync(sub);
    await context.SaveChangesAsync();

    return Results.Created($"api/topics/{id}/subscriptions/{sub.Id}", sub);
});

// Get Subscriber Messages
app.MapGet("api/subscriptions/{id}/messages", async (AppDbContext context, int id) =>
{
    // Check if subscription exists and return 404 if it doesn't
    bool subs = await context.Subscriptions.AnyAsync(s => s.Id == id);
    if (!subs) return Results.NotFound("Subscription not found");

    // Get messages for this subscription that are not already "SENT", and only gets "NEW" messages
    var messages = context.Messages.Where(m => m.SubscriptionId == id && m.MessageStatus != "SENT");
    if (messages.Count() == 0) return Results.NotFound("No new messages");

    // Changes all retrieved messages status that are "NEW" to "REQUESTED"
    // This means subscriber has now requested these messages
    foreach (var msg in messages)
    {
        msg.MessageStatus = "REQUESTED";
    }
    await context.SaveChangesAsync();
    return Results.Ok(messages);
});

// Acknowledge subscribers processed messages
app.MapPost("api/subscriptions/{id}/messages", async (AppDbContext context, int id, int[] confs) =>
{
    bool subs = await context.Subscriptions.AnyAsync(s => s.Id == id);
    if (!subs) return Results.NotFound("Subscription not found");

    // Return 400 status if no message IDs provided
    if (confs.Length <= 0) return Results.BadRequest();

    // Loop through each message ID in the array and find the message by ID
    // If message exists, mark it as "SENT" from "REQUESTED" and save to database
    // Counts all successful acknowledgments
    int count = 0;
    foreach (int i in confs)
    {
        var msg = context.Messages.FirstOrDefault(m => m.Id == i);

        if (msg != null)
        {
            msg.MessageStatus = "SENT";
            await context.SaveChangesAsync();
            count++;
        }
    }
    return Results.Ok($"Acknowledged {count}/{confs.Length} messages");
});

app.Run();


