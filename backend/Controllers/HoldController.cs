using api.DTOs.Holds;
using api.Helper;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    // An "application" is called a "hold" in the code because the seat is only HELD for a while.
    [ApiController]
    public class HoldController : ControllerBase
    {
        private readonly IHoldService _holdService;

        public HoldController(IHoldService holdService)
        {
            _holdService = holdService;
        }

        // Status of one application. The header is required and must match the owner (else 403).
        [HttpGet("/holds/{holdId}")]
        public async Task<ActionResult<HoldInView>> GetHoldById(
            string holdId,
            [FromHeader(Name = RequestHelper.StudentHeader)] string? studentId,
            CancellationToken ct)
        {
            var id = RequestHelper.ParseId(holdId, "application");
            var student = RequestHelper.RequireStudentId(studentId);
            return Ok(await _holdService.GetAsync(id, student, ct));
        }

        // Simulated payment: HELD -> CONFIRMED. Safe to call twice.
        [HttpPost("/holds/{holdId}/confirm")]
        public async Task<ActionResult<HoldInConfirmed>> ConfirmHold(
            string holdId,
            [FromHeader(Name = RequestHelper.StudentHeader)] string? studentId,
            CancellationToken ct)
        {
            var student = RequestHelper.RequireStudentId(studentId);
            var id = RequestHelper.ParseId(holdId, "application");
            return Ok(await _holdService.ConfirmAsync(id, student, ct));
        }

        [HttpDelete("/holds/{holdId}")]
        public async Task<IActionResult> CancelHold(
            string holdId,
            [FromHeader(Name = RequestHelper.StudentHeader)] string? studentId,
            CancellationToken ct)
        {
            var student = RequestHelper.RequireStudentId(studentId);
            var id = RequestHelper.ParseId(holdId, "application");
            await _holdService.CancelAsync(id, student, ct);
            return NoContent();
        }

        // All applications (any status) of the student in the X-Student-Id header.
        [HttpGet("/me/holds")]
        public async Task<ActionResult<List<HoldInView>>> GetMyHolds(
            [FromHeader(Name = RequestHelper.StudentHeader)] string? studentId,
            CancellationToken ct)
        {
            var student = RequestHelper.RequireStudentId(studentId);
            return Ok(await _holdService.ListForStudentAsync(student, ct));
        }
    }
}
