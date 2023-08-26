using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contacts.WebAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    [HttpOptions("register")]
    public Task<IActionResult> Register()
    {
        throw new NotImplementedException();
    }

    [HttpOptions("login-cookie")]
    public Task<IActionResult> LoginCookie()
    {
        throw new NotImplementedException();
    }

    [HttpOptions("logout-cookie")]
    public Task<IActionResult> LogoutCookie()
    {
        throw new NotImplementedException();
    }

    [HttpOptions("access-denied")]
    public IActionResult AccessDenied()
    {
        return Forbid();
    }
}