using api.Models;

namespace api.Interfaces
{
    // Repository = the ONLY layer that talks to the database for classes.
    public interface IClassRepository
    {
        Task<AdmissionClass> CreateClassAsync(AdmissionClass model, CancellationToken ct);
        Task<AdmissionClass?> GetClassByIdAsync(Guid classId, CancellationToken ct);

        // Classes together with live seat numbers (held / confirmed), computed from the holds table.
        Task<List<ClassSeatSummary>> GetAllClassesWithSeatsAsync(DateTime now, CancellationToken ct);
        Task<ClassSeatSummary?> GetClassWithSeatsByIdAsync(Guid classId, DateTime now, CancellationToken ct);
    }
}
