using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PlacementTracker.MinimalApi.Data;
using PlacementTracker.MinimalApi.DTOs;

namespace PlacementTracker.MinimalApi.Endpoints;

// Minimal API endpoint definitions for /api/applications.
// This replaces the controller-based ApplicationsController:
// no controller base class, no ApiController/Route/Http* attributes.
// Endpoints are mapped with app.MapGet/MapPost/MapPut/MapDelete via
// MapApplicationEndpoints() called from Program.cs.
public static class ApplicationEndpoints
{
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/applications");

        // GET /api/applications -> 200 OK
        group.MapGet("/", async (ApplicationDbContext db) =>
        {
            var apps = await db.Applications
                .OrderByDescending(a => a.Id)
                .ToListAsync();
            return Results.Ok(apps.Select(ApplicationResponseDto.FromEntity).ToList());
        });

        // GET /api/applications/{id} -> 200 OK or 404 Not Found
        group.MapGet("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var app = await db.Applications.FindAsync(id);
            if (app is null)
            {
                return Results.NotFound(new { message = $"Application {id} not found." });
            }
            return Results.Ok(ApplicationResponseDto.FromEntity(app));
        });

        // POST /api/applications -> 201 Created or 400 Bad Request
        group.MapPost("/", async (CreateApplicationDto dto, ApplicationDbContext db) =>
        {
            var errors = ValidateDto(dto);
            if (errors is not null)
            {
                return errors;
            }

            var app = new Models.Application
            {
                Company = dto.Company.Trim(),
                Role = dto.Role.Trim(),
                Status = dto.Status,
                AppliedOn = dto.AppliedOn,
                JobUrl = NormalizeOptional(dto.JobUrl),
                Notes = NormalizeOptional(dto.Notes),
                CreatedAt = DateTime.UtcNow
            };

            db.Applications.Add(app);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/applications/{app.Id}",
                ApplicationResponseDto.FromEntity(app));
        });

        // PUT /api/applications/{id} -> 200 OK or 400 / 404
        group.MapPut("/{id:int}", async (int id, UpdateApplicationDto dto, ApplicationDbContext db) =>
        {
            var errors = ValidateDto(dto);
            if (errors is not null)
            {
                return errors;
            }

            var existing = await db.Applications.FindAsync(id);
            if (existing is null)
            {
                return Results.NotFound(new { message = $"Application {id} not found." });
            }

            existing.Company = dto.Company.Trim();
            existing.Role = dto.Role.Trim();
            existing.Status = dto.Status;
            existing.AppliedOn = dto.AppliedOn;
            existing.JobUrl = NormalizeOptional(dto.JobUrl);
            existing.Notes = NormalizeOptional(dto.Notes);

            await db.SaveChangesAsync();

            return Results.Ok(ApplicationResponseDto.FromEntity(existing));
        });

        // DELETE /api/applications/{id} -> 204 No Content or 404 Not Found
        group.MapDelete("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var app = await db.Applications.FindAsync(id);
            if (app is null)
            {
                return Results.NotFound(new { message = $"Application {id} not found." });
            }

            db.Applications.Remove(app);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Manual DataAnnotations validation (Minimal APIs have no [ApiController]
    // automatic ModelState validation). Returns a 400 ValidationProblem result
    // shaped like the controller's ValidationProblem(ModelState), or null when valid.
    public static IResult? ValidateDto<T>(T dto) where T : IValidatableObject
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
        if (results.Count == 0)
        {
            return null;
        }

        var errors = new Dictionary<string, string[]>();
        foreach (var result in results)
        {
            var keys = result.MemberNames.Any() ? result.MemberNames : new[] { string.Empty };
            foreach (var key in keys)
            {
                var name = string.IsNullOrEmpty(key) ? "Error" : key;
                if (errors.TryGetValue(name, out var existing))
                {
                    errors[name] = existing.Append(result.ErrorMessage ?? "Invalid value.").ToArray();
                }
                else
                {
                    errors[name] = new[] { result.ErrorMessage ?? "Invalid value." };
                }
            }
        }

        return Results.ValidationProblem(errors);
    }
}
