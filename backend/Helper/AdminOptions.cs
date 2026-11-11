using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace api.Helper
{
    // Admin login settings. They come ONLY from configuration / environment variables
    // (never from source code):
    //   ADMIN_USERNAME, ADMIN_PASSWORD, JWT_SECRET (at least 32 characters), JWT_EXPIRY_MINUTES (default 60)
    // For `dotnet run` the same values can live in appsettings.Development.json ("Admin" section).
    public sealed class AdminOptions
    {
        public const string Issuer = "school-admission-api";
        public const string Audience = "school-admission-admin";
        public const string AdminRole = "Admin";
        public const int MinSecretLength = 32;
        public const int DefaultExpiryMinutes = 60;

        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string JwtSecret { get; init; } = string.Empty;
        public int ExpiryMinutes { get; init; } = DefaultExpiryMinutes;

        // Admin login only works when all three values are set and the secret is long enough.
        // If not, POST /admin/login answers 503 and POST /classes stays locked: the API is
        // safe by default instead of silently accepting a missing / weak configuration.
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password) &&
            JwtSecret.Length >= MinSecretLength;

        // The key used to sign and validate tokens. If admin is not configured we use a random
        // key nobody knows, so no token can ever validate.
        public SecurityKey SigningKey { get; init; } =
            new SymmetricSecurityKey(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        public static AdminOptions From(IConfiguration config)
        {
            var secret = config["JWT_SECRET"] ?? config["Admin:JwtSecret"] ?? string.Empty;
            var minutes = int.TryParse(config["JWT_EXPIRY_MINUTES"] ?? config["Admin:JwtExpiryMinutes"], out var m) && m > 0
                ? m
                : DefaultExpiryMinutes;

            var options = new AdminOptions
            {
                Username = config["ADMIN_USERNAME"] ?? config["Admin:Username"] ?? string.Empty,
                Password = config["ADMIN_PASSWORD"] ?? config["Admin:Password"] ?? string.Empty,
                JwtSecret = secret,
                ExpiryMinutes = minutes
            };

            if (secret.Length >= MinSecretLength)
            {
                return new AdminOptions
                {
                    Username = options.Username,
                    Password = options.Password,
                    JwtSecret = secret,
                    ExpiryMinutes = minutes,
                    SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
            }

            return options;
        }
    }
}
