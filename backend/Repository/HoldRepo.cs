using api.Database;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace api.Repository
{
    public class HoldRepo : IHoldRepository
    {
        private readonly ApplicationDBContext _context;

        public HoldRepo(ApplicationDBContext context)
        {
            _context = context;
        }

        // ------------------------------------------------------------------
        // Reserving a seat
        // ------------------------------------------------------------------

        // Starts a database transaction. Everything done through the same DbContext
        // (ClassRepo and HoldRepo share one per request) is part of it until Commit.
        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct)
        {
            return await _context.Database.BeginTransactionAsync(ct);
        }

        // "SELECT ... FOR UPDATE" locks this ONE class row until the transaction ends.
        // Other requests for the same class wait in line inside PostgreSQL, so two people
        // can never grab the last seat at the same moment. Other classes are not blocked.
        public async Task LockClassRowAsync(Guid classId, CancellationToken ct)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"AdmissionClasses\" WHERE \"Id\" = {classId} FOR UPDATE", ct);
        }

        public async Task<int> ExpireStaleHoldsForClassAsync(Guid classId, DateTime now, CancellationToken ct)
        {
            return await _context.AdmissionHolds
                .Where(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt <= now)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), ct);
        }

        public async Task<bool> HasActiveHoldAsync(Guid classId, string studentId, CancellationToken ct)
        {
            return await _context.AdmissionHolds.AnyAsync(h =>
                h.ClassId == classId && h.StudentId == studentId &&
                (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed), ct);
        }

        public async Task<int> CountHeldAsync(Guid classId, DateTime now, CancellationToken ct)
        {
            return await _context.AdmissionHolds.CountAsync(h =>
                h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt > now, ct);
        }

        public async Task<int> CountConfirmedAsync(Guid classId, CancellationToken ct)
        {
            return await _context.AdmissionHolds.CountAsync(h =>
                h.ClassId == classId && h.Status == HoldStatus.Confirmed, ct);
        }

        public async Task<AdmissionHold> AddHoldAsync(AdmissionHold hold, CancellationToken ct)
        {
            _context.AdmissionHolds.Add(hold);
            await _context.SaveChangesAsync(ct);
            return hold;
        }

        // ------------------------------------------------------------------
        // Reading
        // ------------------------------------------------------------------
        public async Task<AdmissionHold?> GetHoldByIdAsync(Guid holdId, CancellationToken ct)
        {
            return await _context.AdmissionHolds
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == holdId, ct);
        }

        public async Task<AdmissionHold?> GetHoldWithClassByIdAsync(Guid holdId, CancellationToken ct)
        {
            return await _context.AdmissionHolds
                .AsNoTracking()
                .Include(h => h.Class)
                .FirstOrDefaultAsync(h => h.Id == holdId, ct);
        }

        public async Task<List<AdmissionHold>> GetHoldsByStudentAsync(string studentId, CancellationToken ct)
        {
            return await _context.AdmissionHolds
                .AsNoTracking()
                .Include(h => h.Class)
                .Where(h => h.StudentId == studentId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<AdmissionHold>> GetAllHoldsAsync(CancellationToken ct)
        {
            return await _context.AdmissionHolds
                .AsNoTracking()
                .Include(h => h.Class)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync(ct);
        }

        // ------------------------------------------------------------------
        // State changes: one atomic UPDATE each. If the WHERE does not match
        // (wrong owner, wrong status, time is up) zero rows change and 0 is returned.
        // ------------------------------------------------------------------

        // HELD -> CONFIRMED, only if the owner matches AND the time is not up yet.
        public async Task<int> TryConfirmHoldAsync(Guid holdId, string studentId, DateTime now, CancellationToken ct)
        {
            return await _context.AdmissionHolds
                .Where(h => h.Id == holdId && h.StudentId == studentId &&
                            h.Status == HoldStatus.Held && h.ExpiresAt > now)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(h => h.Status, HoldStatus.Confirmed)
                    .SetProperty(h => h.ConfirmedAt, (DateTime?)now), ct);
        }

        // HELD -> CANCELLED, only if the owner matches AND the time is not up yet.
        public async Task<int> TryCancelHoldAsync(Guid holdId, string studentId, DateTime now, CancellationToken ct)
        {
            return await _context.AdmissionHolds
                .Where(h => h.Id == holdId && h.StudentId == studentId &&
                            h.Status == HoldStatus.Held && h.ExpiresAt > now)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Cancelled), ct);
        }

        // HELD but past its time -> EXPIRED (for one hold).
        public async Task<int> ExpireHoldIfStaleAsync(Guid holdId, DateTime now, CancellationToken ct)
        {
            return await _context.AdmissionHolds
                .Where(h => h.Id == holdId && h.Status == HoldStatus.Held && h.ExpiresAt <= now)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), ct);
        }

        // ------------------------------------------------------------------
        // Background worker
        // ------------------------------------------------------------------
        public async Task<int> ExpireAllStaleHoldsAsync(DateTime now, CancellationToken ct)
        {
            return await _context.AdmissionHolds
                .Where(h => h.Status == HoldStatus.Held && h.ExpiresAt <= now)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), ct);
        }
    }
}
