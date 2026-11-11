using Xunit;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using api.Database;
using api.Models;

namespace SchoolAdmission.Tests;

public record ClassDto(Guid Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);
public record HoldDto(Guid HoldId, Guid ClassId, string StudentId, string Status, DateTime ExpiresAt);
public record ConfirmDto(Guid HoldId, string Status);
public record LoginDto(string Token, string Username, DateTime ExpiresAt);

/// <summary>
/// Boots the real API (in-process) against a REAL PostgreSQL database.
/// Connection string: env TEST_DB_CONNECTION (default: your local PostgreSQL on port 5432, database school_admission_test).
/// The test database is created automatically; it is separate from your real school_admission data.
/// SQLite / in-memory providers are deliberately not used: they have no row-level locks.
/// </summary>
public class AppFixture : IDisposable
{
    public const string DefaultConnection =
        "Host=localhost;Port=5432;Database=school_admission_test;Username=postgres;Password=postgres;Maximum Pool Size=50;Timeout=60";

    public const string AdminUsername = "test-admin";
    public const string AdminPassword = "test-password-123";
    public const string JwtSecret = "test-secret-test-secret-test-secret-123456";

    public WebApplicationFactory<Program> Factory { get; }
    public HttpClient Client { get; }

    public AppFixture()
    {
        var conn = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION") ?? DefaultConnection;

        // Safety: the tests DELETE all rows, so they refuse to run against anything that is not a test database.
        var database = new NpgsqlConnectionStringBuilder(conn).Database ?? string.Empty;
        if (!database.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to run tests against database '{database}'. Use a database whose name contains 'test'.");
        }

        // Environment variables are read when the host builder is created, i.e. before Program.cs runs.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", conn);
        Environment.SetEnvironmentVariable("HOLD_DURATION_SECONDS", "600");
        // Admin login settings used by the tests (the real app reads the same variables).
        Environment.SetEnvironmentVariable("ADMIN_USERNAME", AdminUsername);
        Environment.SetEnvironmentVariable("ADMIN_PASSWORD", AdminPassword);
        Environment.SetEnvironmentVariable("JWT_SECRET", JwtSecret);
        Environment.SetEnvironmentVariable("JWT_EXPIRY_MINUTES", "60");

        Factory = new WebApplicationFactory<Program>();
        Client = Factory.CreateClient(); // starts the app, which applies migrations

        CleanDatabase(); // every run starts from an empty test database -> repeatable

        AdminToken = LoginAsAdminAsync().GetAwaiter().GetResult();
    }

    /// <summary>A valid admin token (obtained through the real POST /admin/login).</summary>
    public string AdminToken { get; }

    private async Task<string> LoginAsAdminAsync()
    {
        var res = await Client.PostAsJsonAsync("/admin/login", new { username = AdminUsername, password = AdminPassword });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<LoginDto>())!.Token;
    }

    /// <summary>POST /classes with an optional bearer token (null = anonymous request).</summary>
    public Task<HttpResponseMessage> PostClassAsync(object body, string? token)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/classes") { Content = JsonContent.Create(body) };
        if (token is not null) req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return Client.SendAsync(req);
    }

    public void Dispose()
    {
        CleanDatabase();
        Client.Dispose();
        Factory.Dispose();
    }

    /// <summary>Removes all classes and holds from the TEST database (holds first: they reference classes).</summary>
    private void CleanDatabase()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        db.AdmissionHolds.ExecuteDelete();
        db.AdmissionClasses.ExecuteDelete();
    }

    // ---------- API helpers ----------
    private static long _studentCounter = 20260000000000000L; // 17 digits
    public static string NewStudentId() => Interlocked.Increment(ref _studentCounter).ToString();

    public async Task<ClassDto> CreateClassAsync(int seats, string? name = null)
    {
        // Creating a class is an admin operation, so the request carries the admin token.
        var res = await PostClassAsync(new { name = name ?? $"Test Class {Guid.NewGuid():N}", totalSeats = seats }, AdminToken);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<ClassDto>())!;
    }

    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? studentId)
    {
        var req = new HttpRequestMessage(method, url);
        if (studentId is not null) req.Headers.Add("X-Student-Id", studentId);
        return Client.SendAsync(req);
    }

    public Task<HttpResponseMessage> ApplyAsync(Guid classId, string studentId) =>
        SendAsync(HttpMethod.Post, $"/classes/{classId}/holds", studentId);

    public async Task<HoldDto> ApplyOkAsync(Guid classId, string studentId)
    {
        var res = await ApplyAsync(classId, studentId);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<HoldDto>())!;
    }

    public Task<HttpResponseMessage> ConfirmAsync(Guid holdId, string studentId) =>
        SendAsync(HttpMethod.Post, $"/holds/{holdId}/confirm", studentId);

    public async Task<ClassDto> GetClassAsync(Guid classId)
    {
        var res = await Client.GetAsync($"/classes/{classId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<ClassDto>())!;
    }

    /// <summary>Insert a hold directly in the database (used to simulate a hold whose time already ran out).</summary>
    public async Task<Guid> SeedHoldAsync(Guid classId, string studentId, string status, DateTime expiresAt)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        var hold = new AdmissionHold
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            StudentId = studentId,
            Status = status,
            CreatedAt = expiresAt.AddMinutes(-10),
            ExpiresAt = expiresAt
        };
        db.AdmissionHolds.Add(hold);
        await db.SaveChangesAsync();
        return hold.Id;
    }
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<AppFixture> { }
