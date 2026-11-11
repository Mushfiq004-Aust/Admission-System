using api.DTOs.Classes;
using api.DTOs.Holds;
using api.Helper;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    // Controllers only deal with HTTP: read the request, call a service, return the response.
    // No business rules and no database code here.
    [ApiController]
    [Route("classes")]
    public class ClassController : ControllerBase
    {
        private readonly IClassService _classService;
        private readonly IHoldService _holdService;

        public ClassController(IClassService classService, IHoldService holdService)
        {
            _classService = classService;
            _holdService = holdService;
        }

        // Admin only: needs the token from POST /admin/login (Authorization: Bearer ...).
        // Every other endpoint in this controller stays open for students.
        [Authorize(Roles = AdminOptions.AdminRole)]
        [HttpPost]
        public async Task<ActionResult<ClassInView>> CreateClass(
            [FromBody] ClassInCreate classInCreateObject,
            [FromHeader(Name = RequestHelper.StudentHeader)] string? studentId,
            CancellationToken ct)
        {
            RequestHelper.OptionalStudentId(studentId); // if sent, it must still be well-formed
            var created = await _classService.CreateAsync(classInCreateObject, ct);
            return Created($"/classes/{created.Id}", created);
        }

        [HttpGet]
        public async Task<ActionResult<List<ClassInView>>> GetAllClasses(
            [FromHeader(Name = RequestHelper.StudentHeader)] string? studentId,
            CancellationToken ct)
        {
            RequestHelper.OptionalStudentId(studentId);
            return Ok(await _classService.ListAsync(ct));
        }

        [HttpGet("{classId}")]
        public async Task<ActionResult<ClassInView>> GetClassById(
            string classId,
            [FromHeader(Name = RequestHelper.StudentHeader)] string? studentId,
            CancellationToken ct)
        {
            RequestHelper.OptionalStudentId(studentId);
            var id = RequestHelper.ParseId(classId, "class");
            return Ok(await _classService.GetAsync(id, ct));
        }

        // Apply: temporarily reserve one seat for the student in the X-Student-Id header.
        [HttpPost("{classId}/holds")]
        public async Task<ActionResult<HoldInCreated>> Apply(
            string classId,
            [FromHeader(Name = RequestHelper.StudentHeader)] string? studentId,
            CancellationToken ct)
        {
            var student = RequestHelper.RequireStudentId(studentId);
            var id = RequestHelper.ParseId(classId, "class");
            var hold = await _holdService.CreateAsync(id, student, ct);
            return Created($"/holds/{hold.HoldId}", hold);
        }
    }
}
