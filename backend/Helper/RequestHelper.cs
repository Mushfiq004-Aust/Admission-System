using System.Text.RegularExpressions;
using api.Exceptions;

namespace api.Helper
{
    // Small checks the controllers share: reading the X-Student-Id header and parsing ids.
    public static class RequestHelper
    {
        public const string StudentHeader = "X-Student-Id";

        // [0-9] (not \d) so only ASCII digits qualify; \z so a trailing newline can't sneak through.
        private static readonly Regex StudentIdPattern = new(@"^[0-9]{17}\z", RegexOptions.Compiled);

        // The header is mandatory: missing or malformed -> 400.
        public static string RequireStudentId(string? headerValue)
        {
            var raw = (headerValue ?? string.Empty).Trim();
            if (raw.Length == 0)
            {
                throw new BadRequestException("MISSING_STUDENT_ID", $"The {StudentHeader} header is required.");
            }
            if (!StudentIdPattern.IsMatch(raw))
            {
                throw new BadRequestException("INVALID_STUDENT_ID",
                    $"The {StudentHeader} header must be a 17-digit birth registration number.");
            }
            return raw;
        }

        // The header is optional: absent -> null; present but malformed -> 400.
        public static string? OptionalStudentId(string? headerValue) =>
            string.IsNullOrWhiteSpace(headerValue) ? null : RequireStudentId(headerValue);

        public static Guid ParseId(string raw, string what)
        {
            if (!Guid.TryParse(raw, out var id))
            {
                throw new BadRequestException("INVALID_ID", $"'{raw}' is not a valid {what} id.");
            }
            return id;
        }
    }
}
