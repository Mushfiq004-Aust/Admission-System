using api.DTOs.Holds;
using api.Exceptions;
using api.Helper;
using api.Interfaces;
using api.Mappers;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace api.Service
{
    // The business rules of admission live here:
    //   - one active application per student per class
    //   - never more held + confirmed seats than the class has
    //   - a hold can only be confirmed / cancelled by its owner, and only before it expires
    public class HoldService : IHoldService
    {
        private readonly IHoldRepository _holdRepo;
        private readonly IClassRepository _classRepo;
        private readonly HoldOptions _options;

        public HoldService(IHoldRepository holdRepository, IClassRepository classRepository, HoldOptions options)
        {
            _holdRepo = holdRepository;
            _classRepo = classRepository;
            _options = options;
        }

        // ------------------------------------------------------------------
        // Reserve a seat ("Apply")
        // ------------------------------------------------------------------
        public async Task<HoldInCreated> CreateAsync(Guid classId, string studentId, CancellationToken ct)
        {
            try
            {
                return await CreateCoreAsync(classId, studentId, ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Backstop: the unique index in the database caught a duplicate active application.
                throw new ConflictException("ALREADY_APPLIED",
                    "This student already has an active application for this class.");
            }
        }

        // Every reservation for a class runs inside a transaction that first LOCKS that class row.
        // Requests for the same class therefore queue up inside PostgreSQL, one at a time,
        // so the seat count we read is always the real one. Different classes never block each other.
        // (ClassRepo and HoldRepo get the same DbContext, so they share this transaction.)
        private async Task<HoldInCreated> CreateCoreAsync(Guid classId, string studentId, CancellationToken ct)
        {
            await using var tx = await _holdRepo.BeginTransactionAsync(ct);

            // 1. Lock the class row. Concurrent requests for this class wait here.
            await _holdRepo.LockClassRowAsync(classId, ct);

            var cls = await _classRepo.GetClassByIdAsync(classId, ct)
                      ?? throw new NotFoundException("CLASS_NOT_FOUND", "Class not found.");

            var now = DateTime.UtcNow;

            // 2. Release stale holds for this class (the background worker also does this).
            await _holdRepo.ExpireStaleHoldsForClassAsync(classId, now, ct);

            // 3. One active application per student per class.
            if (await _holdRepo.HasActiveHoldAsync(classId, studentId, ct))
            {
                throw new ConflictException("ALREADY_APPLIED",
                    "This student already has an active application for this class.");
            }

            // 4. Count occupied seats from the real database state.
            var held = await _holdRepo.CountHeldAsync(classId, now, ct);
            var confirmed = await _holdRepo.CountConfirmedAsync(classId, ct);

            // 5. Full? -> 409 (the transaction rolls back automatically when it is disposed).
            if (held + confirmed >= cls.TotalSeats)
            {
                throw new ConflictException("CLASS_FULL", "No seats are available in this class.");
            }

            // 6. Take the seat.
            var hold = new AdmissionHold
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                StudentId = studentId,
                Status = HoldStatus.Held,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(_options.DurationSeconds)
            };
            await _holdRepo.AddHoldAsync(hold, ct);

            // 7. Commit releases the lock; the next waiting request will see this hold.
            await tx.CommitAsync(ct);

            return hold.ToHoldCreated(cls.Name, now);
        }

        // ------------------------------------------------------------------
        // Read
        // ------------------------------------------------------------------
        public async Task<HoldInView> GetAsync(Guid holdId, string studentId, CancellationToken ct)
        {
            var hold = await _holdRepo.GetHoldWithClassByIdAsync(holdId, ct)
                       ?? throw new NotFoundException("HOLD_NOT_FOUND", "Application not found.");

            if (hold.StudentId != studentId)
            {
                throw new ForbiddenException("NOT_OWNER", "This application belongs to a different student.");
            }

            return hold.ToHoldView(hold.Class!.Name, DateTime.UtcNow);
        }

        public async Task<List<HoldInView>> ListForStudentAsync(string studentId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var holds = await _holdRepo.GetHoldsByStudentAsync(studentId, ct);
            return holds.Select(h => h.ToHoldView(h.Class!.Name, now)).ToList();
        }

        // Admin view: expire overdue reservations before returning the full applicant register.
        public async Task<List<HoldInView>> ListAllForAdminAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            await _holdRepo.ExpireAllStaleHoldsAsync(now, ct);
            var holds = await _holdRepo.GetAllHoldsAsync(ct);
            return holds.Select(h => h.ToHoldView(h.Class!.Name, now)).ToList();
        }

        // ------------------------------------------------------------------
        // Confirm (the simulated payment)
        // ------------------------------------------------------------------
        // One atomic UPDATE does the whole HELD -> CONFIRMED change, and only if the owner matches
        // and the time is not up. The expiry check uses the clock, not the background worker, so a
        // hold can never be confirmed after it expired. Repeating the call matches zero rows
        // (it is no longer HELD), so the seat is counted only once.
        public async Task<HoldInConfirmed> ConfirmAsync(Guid holdId, string studentId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            await _holdRepo.TryConfirmHoldAsync(holdId, studentId, now, ct);

            var hold = await _holdRepo.GetHoldByIdAsync(holdId, ct)
                       ?? throw new NotFoundException("HOLD_NOT_FOUND", "Application not found.");

            if (hold.StudentId != studentId)
            {
                throw new ForbiddenException("NOT_OWNER", "This application belongs to a different student.");
            }

            switch (hold.Status)
            {
                case HoldStatus.Confirmed:
                    // Either we just confirmed it, or an earlier identical request did (idempotent).
                    return hold.ToHoldConfirmed();

                case HoldStatus.Cancelled:
                    throw new ConflictException("HOLD_CANCELLED", "This application was cancelled and cannot be confirmed.");

                default: // HELD but past ExpiresAt, or already EXPIRED
                    await _holdRepo.ExpireHoldIfStaleAsync(hold.Id, now, ct);
                    throw new ConflictException("HOLD_EXPIRED", "The reservation has expired and can no longer be confirmed.");
            }
        }

        // ------------------------------------------------------------------
        // Cancel
        // ------------------------------------------------------------------
        public async Task CancelAsync(Guid holdId, string studentId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            await _holdRepo.TryCancelHoldAsync(holdId, studentId, now, ct);

            var hold = await _holdRepo.GetHoldByIdAsync(holdId, ct)
                       ?? throw new NotFoundException("HOLD_NOT_FOUND", "Application not found.");

            if (hold.StudentId != studentId)
            {
                throw new ForbiddenException("NOT_OWNER", "This application belongs to a different student.");
            }

            switch (hold.Status)
            {
                case HoldStatus.Cancelled:
                    return; // cancelling twice is fine (idempotent)

                case HoldStatus.Confirmed:
                    throw new ConflictException("CANNOT_CANCEL_CONFIRMED",
                        "A confirmed admission cannot be cancelled online. Please contact the school office.");

                default: // HELD but past ExpiresAt, or already EXPIRED
                    await _holdRepo.ExpireHoldIfStaleAsync(hold.Id, now, ct);
                    throw new ConflictException("HOLD_EXPIRED", "The reservation has already expired.");
            }
        }
    }
}
