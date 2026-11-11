using api.DTOs.Classes;

namespace api.Interfaces
{
    // Service = business rules. Controllers call services; services call repositories.
    public interface IClassService
    {
        Task<ClassInView> CreateAsync(ClassInCreate classInCreateObject, CancellationToken ct);
        Task<List<ClassInView>> ListAsync(CancellationToken ct);
        Task<ClassInView> GetAsync(Guid classId, CancellationToken ct);
    }
}
