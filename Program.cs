using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration.AddJsonFile("config.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Configure Entity Framework Core with SQLite and persistent data directory
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "data", "sms.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<SqliteContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

// Apply pending database migrations at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SqliteContext>();
    context.Database.Migrate();
}

// POST /sms endpoint (The Brain)
app.MapPost("/sms", async (HttpRequest request, SqliteContext db, IConfiguration config) =>
{
    var form = await request.ReadFormAsync();
    var body = form["Body"].ToString() ?? string.Empty;
    var from = form["From"].ToString() ?? string.Empty;

    // Optional: Validate AllowedSenderPhone
    var allowedSender = config["AllowedSenderPhone"];
    if (!string.IsNullOrEmpty(allowedSender) && allowedSender != "YOUR_ALLOWED_SENDER_PHONE" && from != allowedSender)
    {
        return Results.Content("<Response></Response>", "application/xml");
    }

    var message = new Message
    {
        Body = body,
        Timestamp = DateTime.UtcNow,
        IsProcessed = false,
        ProjectName = "Inbox"
    };

    var matchId = Regex.Match(body, @"^\[(\d+)\]");
    var matchString = Regex.Match(body, @"^\[([^\]]+)\]");

    if (matchId.Success)
    {
        int parentId = int.Parse(matchId.Groups[1].Value);
        message.ParentId = parentId;
        
        var parent = await db.Messages.FindAsync(parentId);
        message.ProjectName = parent?.ProjectName ?? "Inbox";
    }
    else if (matchString.Success)
    {
        message.ProjectName = matchString.Groups[1].Value;
    }

    db.Messages.Add(message);
    await db.SaveChangesAsync();

    var twiML = $"<Response><Message>Saved to {message.ProjectName} as #{message.Id}</Message></Response>";
    return Results.Content(twiML, "application/xml");
}).DisableAntiforgery();

// GET /messages endpoint (Extension API)
app.MapGet("/messages", async (HttpRequest request, SqliteContext db, IConfiguration config) =>
{
    if (!request.Headers.TryGetValue("X-API-KEY", out var apiKey) || apiKey != config["ServerApiKey"])
    {
        return Results.Unauthorized();
    }

    var allMessages = await db.Messages.OrderBy(m => m.Timestamp).ToListAsync();
    
    var lookup = allMessages.ToLookup(m => m.ParentId);
    
    List<MessageDto> BuildHierarchy(int? parentId)
    {
        return lookup[parentId].Select(m => new MessageDto
        {
            Id = m.Id,
            Body = m.Body,
            ProjectName = m.ProjectName,
            ParentId = m.ParentId,
            Timestamp = m.Timestamp,
            IsProcessed = m.IsProcessed,
            Children = BuildHierarchy(m.Id)
        }).ToList();
    }

    var rootMessages = BuildHierarchy(null);
    return Results.Ok(rootMessages);
});

app.Run();

// Data Models
public class Message
{
    public int Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsProcessed { get; set; }
}

public class MessageDto
{
    public int Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsProcessed { get; set; }
    public List<MessageDto> Children { get; set; } = new();
}

// Database Context
public class SqliteContext : DbContext
{
    public SqliteContext(DbContextOptions<SqliteContext> options) : base(options) { }
    public DbSet<Message> Messages => Set<Message>();
}
