using System.ComponentModel.DataAnnotations;

namespace api.DTOs.Admin
{
    // What the admin login form sends to POST /admin/login.
    public class AdminInLogin
    {
        [Required(ErrorMessage = "username is required.")]
        [StringLength(100, ErrorMessage = "username must be at most 100 characters.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "password is required.")]
        [StringLength(200, ErrorMessage = "password must be at most 200 characters.")]
        public string Password { get; set; } = string.Empty;
    }
}
