using Microsoft.EntityFrameworkCore;
using PlacementTracker.MinimalApi.Data;
using PlacementTracker.MinimalApi.Endpoints;

// This app stores plain calendar dates (AppliedOn, often Unspecified-Kind)
// alongside UTC stamps (CreatedAt). The legacy timestamp behavior accepts
// all DateTime Kinds instead of rejecting mixed Kinds.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Minimal API: no controller services, no controller endpoint mapping.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

// PostgreSQL via configuration / environment variables. No hardcoded secrets.
// Priority: ConnectionStrings__Default env var > appsettings.json > fallback.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
    ?? "Host=127.0.0.1;Port=5432;Database=placement_tracker;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// CORS: allow the Vue dev server and the NGINX entry point to call the API.
// http://localhost:8081 is the Minimal API NGINX origin (the controller-based
// app keeps :8080, so this implementation uses :8081 to run side-by-side).
const string VueDevPolicy = "VueDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(VueDevPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5174", "http://localhost:8081")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(VueDevPolicy);
app.UseAuthorization();

// Minimal API route mapping (replaces app.MapControllers()).
app.MapApplicationEndpoints();

// Create tables if needed (never destructive). Skipped in Testing so tests
// use isolated providers and never touch the real PostgreSQL database.
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();

        // Seed 3 sample records ONLY in Development when the new
        // minimal_api_applications table is empty. The legacy "Applications"
        // table is never read or written here. Idempotent: guarded by Any().
        if (app.Environment.IsDevelopment() && !db.Applications.Any())
        {
            db.Applications.AddRange(
                new PlacementTracker.MinimalApi.Models.Application
                {
                    Company = "Acme Labs",
                    Role = "Python Intern",
                    Status = "Applied",
                    AppliedOn = new DateTime(2026, 9, 16),
                    JobUrl = "https://example.com/jobs/acme-python-intern",
                    Notes = "Sample record.",
                    CreatedAt = DateTime.UtcNow
                },
                new PlacementTracker.MinimalApi.Models.Application
                {
                    Company = "Northstar",
                    Role = "Graduate Engineer",
                    Status = "Interview",
                    AppliedOn = new DateTime(2026, 9, 12),
                    Notes = "Sample record.",
                    CreatedAt = DateTime.UtcNow
                },
                new PlacementTracker.MinimalApi.Models.Application
                {
                    Company = "Contoso",
                    Role = "Backend Intern",
                    Status = "Rejected",
                    AppliedOn = new DateTime(2026, 9, 5),
                    Notes = "Sample record.",
                    CreatedAt = DateTime.UtcNow
                });
            db.SaveChanges();
        }
    }
}

app.Run();

// Needed so WebApplicationFactory (integration tests) can find Program.
public partial class Program { }
