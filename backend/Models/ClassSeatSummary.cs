namespace api.Models
{
    // NOT a database table. It is the result of one query that asks the database
    // "give me each class together with how many seats are held / confirmed".
    // The repository fills it, the mapper turns it into a ClassInView.
    public class ClassSeatSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public DateTime CreatedAt { get; set; }
        public int HeldSeats { get; set; }
        public int ConfirmedSeats { get; set; }
    }
}
