namespace api.DTOs.Holds
{
    // Response of POST /holds/{id}/confirm (the "Pay Admission Fee" button).
    public class HoldInConfirmed
    {
        public Guid HoldId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ConfirmedAt { get; set; }
    }
}
