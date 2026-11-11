namespace api.DTOs.Holds
{
    // Response of GET /holds/{id} and the items of GET /me/holds.
    public class HoldInView
    {
        public Guid HoldId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }

        // The server's clock at response time, so the browser can correct for a wrong device clock.
        public DateTime ServerTime { get; set; }
    }
}
