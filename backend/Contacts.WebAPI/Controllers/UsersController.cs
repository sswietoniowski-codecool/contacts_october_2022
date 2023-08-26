using Contacts.WebAPI.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Contacts.WebAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserManager<User> userManager, SignInManager<User> signInManager,
        ILogger<UsersController> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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