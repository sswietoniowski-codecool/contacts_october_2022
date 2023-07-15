using Contacts.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Contacts.WebAPI.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return new JsonResult(
            DataService.Instance.Contacts
        );
    }
}