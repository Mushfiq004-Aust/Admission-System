namespace api.Models
{
    // A student's application (a temporary seat reservation) for a class.
    // This becomes the "AdmissionHolds" table in the database.
    public class AdmissionHold
    {
        public Guid Id { get; set; }

        public Guid ClassId { get; set; }
        public AdmissionClass? Class { get; set; }

        // 17-digit birth registration number. There is no Student table;
        // this number is the only "identity" a student has.
        public string StudentId { get; set; } = string.Empty;

        public string Status { get; set; } = HoldStatus.Held;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }
}
