using api.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace api.Interfaces
{
    // Repository = the ONLY layer that talks to the database for holds (applications).
    // Each method does one small database job. The business rules live in HoldService.
    public interface IHoldRepository
    {
        // ---- used while reserving a seat (all inside one database transaction) ----
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
        Task LockClassRowAsync(Guid classId, CancellationToken ct);
        Task<int> ExpireStaleHoldsForClassAsync(Guid classId, DateTime now, CancellationToken ct);
        Task<bool> HasActiveHoldAsync(Guid classId, string studentId, CancellationToken ct);
        Task<int> CountHeldAsync(Guid classId, DateTime now, CancellationToken ct);
        Task<int> CountConfirmedAsync(Guid classId, CancellationToken ct);
        Task<AdmissionHold> AddHoldAsync(AdmissionHold hold, CancellationToken ct);

        // ---- reading ----
        Task<AdmissionHold?> GetHoldByIdAsync(Guid holdId, CancellationToken ct);
        Task<AdmissionHold?> GetHoldWithClassByIdAsync(Guid holdId, CancellationToken ct);
        Task<List<AdmissionHold>> GetHoldsByStudentAsync(string studentId, CancellationToken ct);
        Task<List<AdmissionHold>> GetAllHoldsAsync(CancellationToken ct);

        // ---- state changes: each is ONE atomic UPDATE and returns how many rows it changed ----
        Task<int> TryConfirmHoldAsync(Guid holdId, string studentId, DateTime now, CancellationToken ct);
        Task<int> TryCancelHoldAsync(Guid holdId, string studentId, DateTime now, CancellationToken ct);
        Task<int> ExpireHoldIfStaleAsync(Guid holdId, DateTime now, CancellationToken ct);

        // ---- used by the background worker ----
        Task<int> ExpireAllStaleHoldsAsync(DateTime now, CancellationToken ct);
    }
}
