using api.DTOs.Admin;

namespace api.Interfaces
{
    public interface IAdminAuthService
    {
        // Checks the username/password against the configured admin account and returns a signed token.
        AdminInView Login(AdminInLogin login);
    }
}
