using api.DTOs.Classes;
using api.Exceptions;
using api.Interfaces;
using api.Mappers;

namespace api.Service
{
    public class ClassService : IClassService
    {
        private readonly IClassRepository _classRepo;

        public ClassService(IClassRepository classRepository)
        {
            _classRepo = classRepository;
        }

        public async Task<ClassInView> CreateAsync(ClassInCreate classInCreateObject, CancellationToken ct)
        {
            var model = classInCreateObject.ToClassCreate();
            await _classRepo.CreateClassAsync(model, ct);
            return model.ToClassView();
        }

        public async Task<List<ClassInView>> ListAsync(CancellationToken ct)
        {
            var rows = await _classRepo.GetAllClassesWithSeatsAsync(DateTime.UtcNow, ct);
            return rows.Select(r => r.ToClassView()).ToList();
        }

        public async Task<ClassInView> GetAsync(Guid classId, CancellationToken ct)
        {
            var row = await _classRepo.GetClassWithSeatsByIdAsync(classId, DateTime.UtcNow, ct)
                      ?? throw new NotFoundException("CLASS_NOT_FOUND", "Class not found.");
            return row.ToClassView();
        }
    }
}
