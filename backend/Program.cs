using api.Database;
using api.DTOs.Common;
using api.Helper;
using api.Interfaces;
using api.Middleware;
using api.Repository;
using api.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// This is the starting point of the API.
var builder = WebApplication.CreateBuilder(args);

// Swagger gives you a web page (/swagger) where you can try every endpoint without any extra tool.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Adds the "Authorize" button in Swagger: paste the token from POST /admin/login to try POST /classes.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Admin token from POST /admin/login"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Allows the React dev server (running on its own port) to call this API.
// Without this, the browser blocks every request from the frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        // Frontend:Url may list several addresses separated by commas
        // (e.g. the Vite dev server on 5173 AND the Docker frontend on 3000).
        var origins = (builder.Configuration["Frontend:Url"] ?? "http://localhost:5173")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// Reservation settings (how long a seat is held) come from appsettings.json -> "Hold".
builder.Services.AddSingleton(sp => HoldOptions.From(sp.GetRequiredService<IConfiguration>()));

// Admin login: credentials and the token secret come from environment variables (see Helper/AdminOptions.cs).
var adminOptions = AdminOptions.From(builder.Configuration);
builder.Services.AddSingleton(adminOptions);

// The backend verifies the signed token itself on every protected request.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep the claim names "sub" and "role" as they are in the token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = adminOptions.SigningKey,
            ValidateIssuer = true,
            ValidIssuer = AdminOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = AdminOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            // No / bad / expired token -> the same JSON error shape as every other error.
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ErrorInView("UNAUTHORIZED", "Admin login required."));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ErrorInView("FORBIDDEN", "You are not allowed to do this."));
            }
        };
    });
builder.Services.AddAuthorization();

// Add the ApplicationDBContext to the services collection, so that it can be injected into the repositories.
builder.Services.AddDbContext<ApplicationDBContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
    options.UseNpgsql(connectionString);
});

// Repositories (database access) ...
builder.Services.AddScoped<IClassRepository, ClassRepo>();
builder.Services.AddScoped<IHoldRepository, HoldRepo>();

// ... services (business rules) ...
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IHoldService, HoldService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();

// ... and the background worker that expires old holds.
builder.Services.AddHostedService<HoldExpirationService>();

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Validation / malformed-JSON problems use the same error shape as every other error.
        options.InvalidModelStateResponseFactory = context =>
        {
            var messages = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid request body." : e.ErrorMessage)
                .Distinct();
            return new BadRequestObjectResult(new ErrorInView("VALIDATION_ERROR", string.Join(" ", messages)));
        };
    });

//where the app is build after all that configurations
var app = builder.Build();

// Create the database + tables if they are missing (see Helper/DatabaseMigrator.cs).
await app.MigrateWithRetryAsync();

if (!adminOptions.IsConfigured)
{
    app.Logger.LogWarning(
        "Admin login is NOT configured (set ADMIN_USERNAME, ADMIN_PASSWORD and a JWT_SECRET of at least {Min} characters). " +
        "POST /classes will stay locked until it is.", AdminOptions.MinSecretLength);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Must come early so the CORS check happens on the way in.
app.UseCors("AllowReact");

// Turns exceptions into clean JSON errors.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Who is calling (token check) and are they allowed (only POST /classes and GET /admin/me ask for the admin role).
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Map the controllers to the endpoints
app.MapControllers();

app.Run();

// Needed so the test project can start the whole API with WebApplicationFactory<Program>.
public partial class Program { }
