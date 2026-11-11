using Xunit;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using api.Helper;
using Microsoft.IdentityModel.Tokens;

namespace SchoolAdmission.Tests;

/// <summary>
/// Admin login + protection of POST /classes, and proof that every student endpoint still works
/// with NO admin credentials at all.
/// </summary>
[Collection("api")]
public class AdminAuthTests
{
    private readonly AppFixture _app;

    public AdminAuthTests(AppFixture app) => _app = app;

    // ---------------------------------------------------------------- login
    [Fact]
    public async Task AdminLogin_WithCorrectCredentials_ReturnsSignedToken()
    {
        var res = await _app.Client.PostAsJsonAsync("/admin/login",
            new { username = AppFixture.AdminUsername, password = AppFixture.AdminPassword });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = (await res.Content.ReadFromJsonAsync<LoginDto>())!;
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.Equal(3, body.Token.Split('.').Length); // header.payload.signature
        Assert.Equal(AppFixture.AdminUsername, body.Username);
        Assert.InRange((body.ExpiresAt - DateTime.UtcNow).TotalMinutes, 55, 61);

        // The password must never come back in the response.
        var raw = await (await _app.Client.PostAsJsonAsync("/admin/login",
            new { username = AppFixture.AdminUsername, password = AppFixture.AdminPassword })).Content.ReadAsStringAsync();
        Assert.DoesNotContain(AppFixture.AdminPassword, raw);
    }

    [Theory]
    [InlineData("test-admin", "wrong-password")]
    [InlineData("someone-else", "test-password-123")]
    [InlineData("admin", "admin")]
    public async Task AdminLogin_WithWrongCredentials_Returns401(string username, string password)
    {
        var res = await _app.Client.PostAsJsonAsync("/admin/login", new { username, password });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var text = await res.Content.ReadAsStringAsync();
        Assert.Contains("INVALID_CREDENTIALS", text);
        Assert.DoesNotContain("\"token\"", text);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"username\":\"test-admin\"}")]
    [InlineData("{\"password\":\"test-password-123\"}")]
    [InlineData("{\"username\":\"\",\"password\":\"\"}")]
    public async Task AdminLogin_WithMissingFields_Returns400(string json)
    {
        var res = await _app.Client.PostAsync("/admin/login",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---------------------------------------------------------------- POST /classes is protected
    [Fact]
    public async Task CreateClass_WithoutToken_Returns401_AndCreatesNothing()
    {
        var name = $"Anonymous attempt {Guid.NewGuid():N}";

        var res = await _app.PostClassAsync(new { name, totalSeats = 10 }, token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var classes = await _app.Client.GetFromJsonAsync<List<ClassDto>>("/classes");
        Assert.DoesNotContain(classes!, c => c.Name == name);
    }

    [Fact]
    public async Task CreateClass_WithStudentHeaderOnly_Returns401()
    {
        // A student identity is NOT an admin credential.
        var req = new HttpRequestMessage(HttpMethod.Post, "/classes") { Content = JsonContent.Create(new { name = "Sneaky", totalSeats = 5 }) };
        req.Headers.Add("X-Student-Id", AppFixture.NewStudentId());

        var res = await _app.Client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task CreateClass_WithGarbageToken_Returns401()
    {
        var res = await _app.PostClassAsync(new { name = "X", totalSeats = 5 }, "not.a.jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task CreateClass_WithTokenSignedByAnotherSecret_Returns401()
    {
        var forged = MakeToken("another-secret-another-secret-another-secret-1", AdminOptions.AdminRole, TimeSpan.FromHours(1));
        var res = await _app.PostClassAsync(new { name = "Forged", totalSeats = 5 }, forged);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task CreateClass_WithExpiredToken_Returns401()
    {
        var expired = MakeToken(AppFixture.JwtSecret, AdminOptions.AdminRole, TimeSpan.FromHours(-1));
        var res = await _app.PostClassAsync(new { name = "Expired", totalSeats = 5 }, expired);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task CreateClass_WithValidTokenButNotAdminRole_Returns403()
    {
        var token = MakeToken(AppFixture.JwtSecret, "Student", TimeSpan.FromHours(1));
        var res = await _app.PostClassAsync(new { name = "Not admin", totalSeats = 5 }, token);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CreateClass_WithTokenFromRealLogin_Returns201()
    {
        var name = $"Class 6 - Morning Shift {Guid.NewGuid():N}";

        var res = await _app.PostClassAsync(new { name, totalSeats = 120 }, _app.AdminToken);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = (await res.Content.ReadFromJsonAsync<ClassDto>())!;
        Assert.Equal(name, created.Name);
        Assert.Equal(120, created.TotalSeats);
    }

    // ---------------------------------------------------------------- /admin/me
    [Fact]
    public async Task AdminMe_RequiresAValidAdminToken()
    {
        var anonymous = await _app.Client.GetAsync("/admin/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var req = new HttpRequestMessage(HttpMethod.Get, "/admin/me");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _app.AdminToken);
        var authorised = await _app.Client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, authorised.StatusCode);
        Assert.Contains(AppFixture.AdminUsername, await authorised.Content.ReadAsStringAsync());
    }

    // ---------------------------------------------------------------- student API is unchanged
    [Fact]
    public async Task StudentEndpoints_AllWorkWithoutAnyAdminCredentials()
    {
        // The class itself needs an admin to exist; everything after that is done as a student with NO Authorization header.
        var cls = await _app.CreateClassAsync(3, "Class 7 - Day Shift");
        var student = AppFixture.NewStudentId();

        Assert.Equal(HttpStatusCode.OK, (await _app.Client.GetAsync("/classes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _app.Client.GetAsync($"/classes/{cls.Id}")).StatusCode);

        var apply = await _app.ApplyAsync(cls.Id, student);
        Assert.Equal(HttpStatusCode.Created, apply.StatusCode);
        var hold = (await apply.Content.ReadFromJsonAsync<HoldDto>())!;

        Assert.Equal(HttpStatusCode.OK, (await _app.SendAsync(HttpMethod.Get, $"/holds/{hold.HoldId}", student)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _app.SendAsync(HttpMethod.Get, "/me/holds", student)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _app.ConfirmAsync(hold.HoldId, student)).StatusCode);

        // cancel path on a second student
        var other = AppFixture.NewStudentId();
        var second = await _app.ApplyOkAsync(cls.Id, other);
        Assert.Equal(HttpStatusCode.NoContent, (await _app.SendAsync(HttpMethod.Delete, $"/holds/{second.HoldId}", other)).StatusCode);
    }

    [Fact]
    public async Task AdminApplications_RequiresAdminToken_AndListsApplicantsWithStatuses()
    {
        var anonymous = await _app.Client.GetAsync("/admin/applications");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var cls = await _app.CreateClassAsync(2, "Applicant register test class");
        var student = AppFixture.NewStudentId();
        var hold = await _app.ApplyOkAsync(cls.Id, student);

        var req = new HttpRequestMessage(HttpMethod.Get, "/admin/applications");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _app.AdminToken);
        var response = await _app.Client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(student, body);
        Assert.Contains(hold.HoldId.ToString(), body);
        Assert.Contains("HELD", body);
    }

    // ---------------------------------------------------------------- helper
    private static string MakeToken(string secret, string role, TimeSpan lifetime)
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(lifetime);
        // For an already-expired token, notBefore must be earlier than expires.
        var notBefore = lifetime < TimeSpan.Zero ? expires.AddMinutes(-10) : now;

        var token = new JwtSecurityToken(
            issuer: AdminOptions.Issuer,
            audience: AdminOptions.Audience,
            claims: new[] { new Claim("sub", AppFixture.AdminUsername), new Claim("role", role) },
            notBefore: notBefore,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
