namespace api.DTOs.Admin
{
    // Response of a successful admin login. The password is never sent back; only a signed token.
    public class AdminInView
    {
        public string Token { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        // UTC time at which the token stops being accepted.
        public DateTime ExpiresAt { get; set; }
    }
}
