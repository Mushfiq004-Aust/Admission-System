namespace api.DTOs.Holds
{
    // Response of POST /classes/{classId}/holds (the "Apply" button).
    public class HoldInCreated
    {
        public Guid HoldId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime ServerTime { get; set; }
    }
}
