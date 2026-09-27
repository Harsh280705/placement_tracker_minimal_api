using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlacementTracker.MinimalApi.Data;
using PlacementTracker.MinimalApi.Endpoints;

namespace PlacementTracker.MinimalApi.Tests;

// Minimal-API-specific tests: endpoint wiring, validation helper,
// table mapping, and absence of controller-based infrastructure.
public class MinimalApiTests
{
    [Fact]
    public void DbContext_maps_to_minimal_api_table_not_legacy_table()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=5432;Database=placement_tracker;Username=postgres;Password=postgres")
            .Options;
        using var db = new ApplicationDbContext(options);
        var entityType = db.Model.FindEntityType(typeof(Models.Application));
        Assert.NotNull(entityType);
        Assert.Equal("minimal_api_applications", entityType!.GetTableName());
        Assert.Equal("minimal_api_applications", ApplicationDbContext.TableName);
    }

    [Fact]
    public void Backend_uses_no_controller_infrastructure()
    {
        var assembly = typeof(Program).Assembly;
        var controllerTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Empty(controllerTypes);

        var sourceFiles = Directory.GetFiles(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "backend"),
            "*.cs", SearchOption.AllDirectories);
        foreach (var file in sourceFiles)
        {
            var text = File.ReadAllText(file);
            // Strip // line comments so explanatory comments don't trip the check.
            var code = string.Join("\n", text.Split('\n')
                .Select(line => line.Split(new[] { "//" }, StringSplitOptions.None)[0]));
            Assert.DoesNotContain("ControllerBase", code);
            Assert.DoesNotContain("[ApiController]", code);
            Assert.DoesNotContain("[HttpGet]", code);
            Assert.DoesNotContain("[HttpPost]", code);
            Assert.DoesNotContain("[HttpPut]", code);
            Assert.DoesNotContain("[HttpDelete]", code);
            Assert.DoesNotContain("MapControllers", code);
        }
    }

    [Fact]
    public void Endpoints_file_uses_minimal_api_mapping()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "backend", "Endpoints");
        var file = Path.Combine(dir, "ApplicationEndpoints.cs");
        Assert.True(File.Exists(file), "Endpoints/ApplicationEndpoints.cs must exist.");
        var text = File.ReadAllText(file);
        Assert.Contains("MapGet", text);
        Assert.Contains("MapPost", text);
        Assert.Contains("MapPut", text);
        Assert.Contains("MapDelete", text);
    }

    [Fact]
    public void ValidateDto_valid_returns_null()
    {
        var result = ApplicationEndpoints.ValidateDto(TestHelpers.ValidCreateDto());
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateDto_invalid_returns_400_validation_problem()
    {
        var dto = TestHelpers.ValidCreateDto();
        dto.Company = "";
        dto.Status = "Hired";

        var result = ApplicationEndpoints.ValidateDto(dto);
        Assert.NotNull(result);

        // Execute the result through a minimal-routing context to inspect the status code.
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        await result!.ExecuteAsync(httpContext);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InMemory_crud_roundtrip_against_new_context()
    {
        using var db = TestHelpers.CreateDb();

        // Create
        var app = TestHelpers.ValidApplication();
        db.Applications.Add(app);
        await db.SaveChangesAsync();
        Assert.Equal(1, db.Applications.Count());

        // Read (ordered desc like GET all)
        var all = await db.Applications.OrderByDescending(a => a.Id).ToListAsync();
        Assert.Single(all);

        // Update
        var existing = await db.Applications.FindAsync(app.Id);
        Assert.NotNull(existing);
        existing!.Company = "Contoso";
        await db.SaveChangesAsync();
        Assert.Equal("Contoso", db.Applications.Single().Company);

        // Delete
        db.Applications.Remove(existing);
        await db.SaveChangesAsync();
        Assert.Empty(db.Applications.ToList());
    }
}
