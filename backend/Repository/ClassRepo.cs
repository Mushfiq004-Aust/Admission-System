using api.Database;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class ClassRepo : IClassRepository
    {
        private readonly ApplicationDBContext _context;

        public ClassRepo(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<AdmissionClass> CreateClassAsync(AdmissionClass model, CancellationToken ct)
        {
            _context.AdmissionClasses.Add(model);
            await _context.SaveChangesAsync(ct);
            return model;
        }

        public async Task<AdmissionClass?> GetClassByIdAsync(Guid classId, CancellationToken ct)
        {
            return await _context.AdmissionClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == classId, ct);
        }

        public Task<List<ClassSeatSummary>> GetAllClassesWithSeatsAsync(DateTime now, CancellationToken ct)
        {
            return QueryWithSeatsAsync(null, now, ct);
        }

        public async Task<ClassSeatSummary?> GetClassWithSeatsByIdAsync(Guid classId, DateTime now, CancellationToken ct)
        {
            var rows = await QueryWithSeatsAsync(classId, now, ct);
            return rows.FirstOrDefault();
        }

        // Seat numbers are always DERIVED from the holds table (never stored):
        //   held      = HELD rows that have not expired yet
        //   confirmed = CONFIRMED rows
        // Expiry is checked against the clock here, so a stale HELD row that the
        // background worker has not flipped yet never reduces availability.
        private async Task<List<ClassSeatSummary>> QueryWithSeatsAsync(Guid? classId, DateTime now, CancellationToken ct)
        {
            var query = _context.AdmissionClasses.AsNoTracking();

            if (classId.HasValue)
            {
                var wanted = classId.Value;
                query = query.Where(c => c.Id == wanted);
            }

            return await query
                .OrderBy(c => c.Name)
                .ThenBy(c => c.CreatedAt)
                .Select(c => new ClassSeatSummary
                {
                    Id = c.Id,
                    Name = c.Name,
                    TotalSeats = c.TotalSeats,
                    CreatedAt = c.CreatedAt,
                    HeldSeats = c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                    ConfirmedSeats = c.Holds.Count(h => h.Status == HoldStatus.Confirmed)
                })
                .ToListAsync(ct);
        }
    }
}
