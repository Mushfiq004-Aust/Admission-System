namespace api.Models
{
    // One admission class. Each shift is its own row with its own capacity
    // (e.g. "Class 6 - Morning Shift" and "Class 6 - Day Shift" are two rows).
    // This becomes the "AdmissionClasses" table in the database.
    public class AdmissionClass
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public DateTime CreatedAt { get; set; }

        // One class has many holds (applications).
        public List<AdmissionHold> Holds { get; set; } = new();
    }
}
