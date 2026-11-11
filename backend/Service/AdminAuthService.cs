using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using api.DTOs.Admin;
using api.Exceptions;
using api.Helper;
using api.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace api.Service
{
    // Admin authentication: verify the configured credentials, then issue a signed, expiring JWT.
    // The backend (not the frontend) is what decides who is an admin: POST /classes requires this token.
    public class AdminAuthService : IAdminAuthService
    {
        private readonly AdminOptions _options;

        public AdminAuthService(AdminOptions options)
        {
            _options = options;
        }

        public AdminInView Login(AdminInLogin login)
        {
            if (!_options.IsConfigured)
            {
                throw new ServiceUnavailableException("ADMIN_NOT_CONFIGURED",
                    "Admin login is not configured on the server (ADMIN_USERNAME, ADMIN_PASSWORD, JWT_SECRET).");
            }

            // '&' (not '&&') so both checks always run: the time taken does not reveal which part was wrong.
            var userOk = FixedTimeEquals(login.Username, _options.Username);
            var passOk = FixedTimeEquals(login.Password, _options.Password);
            if (!(userOk & passOk))
            {
                throw new UnauthorizedException("INVALID_CREDENTIALS", "Wrong username or password.");
            }

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_options.ExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: AdminOptions.Issuer,
                audience: AdminOptions.Audience,
                claims: new[]
                {
                    new Claim("sub", _options.Username),
                    new Claim("role", AdminOptions.AdminRole)
                },
                notBefore: now,
                expires: expires,
                signingCredentials: new SigningCredentials(_options.SigningKey, SecurityAlgorithms.HmacSha256));

            return new AdminInView
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Username = _options.Username,
                ExpiresAt = expires
            };
        }

        // Compares two strings without leaking, through timing, how many leading characters matched.
        private static bool FixedTimeEquals(string a, string b)
        {
            var hashA = SHA256.HashData(Encoding.UTF8.GetBytes(a));
            var hashB = SHA256.HashData(Encoding.UTF8.GetBytes(b));
            return CryptographicOperations.FixedTimeEquals(hashA, hashB);
        }
    }
}
