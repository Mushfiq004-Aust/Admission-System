using api.Database;
using Microsoft.EntityFrameworkCore;

namespace api.Helper
{
    // Creates the database (if it does not exist) and applies all migrations (creates the tables)
    // every time the API starts. So you never have to run "dotnet ef database update" by hand.
    public static class DatabaseMigrator
    {
        public static async Task MigrateWithRetryAsync(this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
            const int maxAttempts = 15;

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    using var scope = app.Services.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database is ready and migrations are applied.");
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    logger.LogWarning(
                        "Cannot reach the database yet (attempt {Attempt}/{Max}): {Message}",
                        attempt, maxAttempts, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Could not connect to PostgreSQL. Is it installed and running? " +
                        "Check ConnectionStrings:DefaultConnection in backend/appsettings.json (host, port, username, password).");
                    throw;
                }
            }
        }
    }
}
