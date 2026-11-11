using api.DTOs.Admin;
using api.Helper;
using api.DTOs.Holds;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    // Admin login for the school staff. Students never use these endpoints.
    [ApiController]
    [Route("admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminAuthService _adminAuthService;
        private readonly IHoldService _holdService;

        public AdminController(IAdminAuthService adminAuthService, IHoldService holdService)
        {
            _adminAuthService = adminAuthService;
            _holdService = holdService;
        }

        // 200 + token on success, 401 for wrong credentials, 400 for an empty form.
        [HttpPost("login")]
        public ActionResult<AdminInView> Login([FromBody] AdminInLogin login)
        {
            return Ok(_adminAuthService.Login(login));
        }

        // Lets the admin website ask "is my token still valid?" (401 if not).
        [Authorize(Roles = AdminOptions.AdminRole)]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new { username = User.FindFirst("sub")?.Value });
        }

        // Full applicant register. Birth registration numbers are visible only to authenticated admins.
        [Authorize(Roles = AdminOptions.AdminRole)]
        [HttpGet("applications")]
        public async Task<ActionResult<List<HoldInView>>> Applications(CancellationToken ct)
        {
            return Ok(await _holdService.ListAllForAdminAsync(ct));
        }
    }
}
