using api.DTOs.Classes;
using api.Models;

namespace api.Mappers
{
    // Mappers convert between DTOs (what travels over HTTP) and Models (what lives in the database).
    // They are "extension methods", so you can write:  dto.ToClassCreate()  or  summary.ToClassView()
    public static class ClassMapper
    {
        // DTO from the client -> new database row.
        public static AdmissionClass ToClassCreate(this ClassInCreate dto)
        {
            return new AdmissionClass
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                TotalSeats = dto.TotalSeats,
                CreatedAt = DateTime.UtcNow
            };
        }

        // A brand-new class: nothing is held or confirmed yet.
        public static ClassInView ToClassView(this AdmissionClass model)
        {
            return Build(model.Id, model.Name, model.TotalSeats, 0, 0, model.CreatedAt);
        }

        // A class together with its live seat numbers.
        public static ClassInView ToClassView(this ClassSeatSummary summary)
        {
            return Build(summary.Id, summary.Name, summary.TotalSeats,
                summary.HeldSeats, summary.ConfirmedSeats, summary.CreatedAt);
        }

        private static ClassInView Build(Guid id, string name, int totalSeats, int held, int confirmed, DateTime createdAt)
        {
            return new ClassInView
            {
                Id = id,
                Name = name,
                TotalSeats = totalSeats,
                HeldSeats = held,
                ConfirmedSeats = confirmed,
                AvailableSeats = Math.Max(0, totalSeats - held - confirmed),
                CreatedAt = createdAt
            };
        }
    }
}
