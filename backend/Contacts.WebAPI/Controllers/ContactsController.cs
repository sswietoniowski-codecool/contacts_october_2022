using Contacts.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Contacts.WebAPI.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly DataService _dataService;

    public ContactsController(DataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return new JsonResult(
            _dataService.Contacts
        );
    }
}