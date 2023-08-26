using System.Security.Claims;
using Contacts.WebAPI.Domain;
using Contacts.WebAPI.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

    private IActionResult ReportErrors(IdentityResult result, User user)
    {
        var errors = result.Errors.Select(e => e.Description);

        _logger.LogWarning("User {userName} ({email}) creation failed. Errors: {errors}", user.UserName,
            user.Email, errors);

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return BadRequest(ModelState);
    }

    [HttpOptions("register")]
    [ResponseCache(CacheProfileName = "NoCache")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] UserForRegistrationDto userForRegistrationDto)
    {
        var user = new User();
        user.UserName = userForRegistrationDto.UserName;
        user.Email = userForRegistrationDto.Email;

        var result = await _userManager.CreateAsync(user, userForRegistrationDto.Password);

        if (!result.Succeeded)
        {
            return ReportErrors(result, user);
        }

        _logger.LogInformation($"User {userForRegistrationDto.UserName} created.");

        result = await _userManager.AddToRolesAsync(user, userForRegistrationDto.Roles);

        if (!result.Succeeded)
        {
            return ReportErrors(result, user);
        }

        _logger.LogInformation($"User {userForRegistrationDto.UserName} assigned to roles.");

        return Accepted($"User '{user.UserName}' has been created");
    }

    [HttpOptions("login-cookie")]
    [ResponseCache(CacheProfileName = "NoCache")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginCookie([FromBody] UserForLoginWithCookieDto userForLoginDto)
    {
        var user = await _userManager.FindByNameAsync(userForLoginDto.UserName);

        if (user is null)
        {
            _logger.LogWarning("User {userName} not found.", userForLoginDto.UserName);

            return Unauthorized(userForLoginDto);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, userForLoginDto.Password, false);

        if (!result.Succeeded)
        {
            _logger.LogWarning("User {userName} login failed.", userForLoginDto.UserName);

            return Unauthorized(userForLoginDto);
        }

        _logger.LogInformation("User {userName} logged in.", userForLoginDto.UserName);

        var claims = new List<Claim>();

        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, user.UserName!));

        var roles = await _userManager.GetRolesAsync(user);

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = userForLoginDto.RememberMe,
            });

        return Accepted();
    }

    [HttpOptions("logout-cookie")]
    [ResponseCache(CacheProfileName = "NoCache")]
    public async Task<IActionResult> LogoutCookie()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Accepted();
    }

    [HttpOptions("access-denied")]
    [ResponseCache(CacheProfileName = "NoCache")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return Forbid();
    }
}