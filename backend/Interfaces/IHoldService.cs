using api.DTOs.Holds;

namespace api.Interfaces
{
    public interface IHoldService
    {
        Task<HoldInCreated> CreateAsync(Guid classId, string studentId, CancellationToken ct);
        Task<HoldInView> GetAsync(Guid holdId, string studentId, CancellationToken ct);
        Task<HoldInConfirmed> ConfirmAsync(Guid holdId, string studentId, CancellationToken ct);
        Task CancelAsync(Guid holdId, string studentId, CancellationToken ct);
        Task<List<HoldInView>> ListForStudentAsync(string studentId, CancellationToken ct);
        Task<List<HoldInView>> ListAllForAdminAsync(CancellationToken ct);
    }
}
