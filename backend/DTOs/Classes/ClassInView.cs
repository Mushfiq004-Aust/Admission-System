namespace api.DTOs.Classes
{
    // What the API sends back for a class (with live seat numbers).
    public class ClassInView
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public int HeldSeats { get; set; }
        public int ConfirmedSeats { get; set; }
        public int AvailableSeats { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
