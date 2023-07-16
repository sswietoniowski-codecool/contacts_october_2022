﻿using Contacts.WebAPI.Domain;
using Contacts.WebAPI.DTOs;
using Contacts.WebAPI.Infrastructure;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Contacts.WebAPI.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly DataService _dataService;
    private readonly ContactsDbContext _dbContext;

    public ContactsController(DataService dataService, ContactsDbContext dbContext)
    {
        _dataService = dataService;
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    // GET api/contacts?search=ski
    [HttpGet]
    public ActionResult<IEnumerable<ContactDto>> GetContacts([FromQuery] string? search)
    {
        var query = _dbContext.Contacts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.LastName.Contains(search));
        }

        var contactsDto = query
            .Select(c => new ContactDto()
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email
            });

        return Ok(contactsDto);
    }

    // GET api/contacts/1
    // GET api/contacts/{id}
    [HttpGet("{id:int}")]
    public ActionResult<ContactDto> GetContact(int id)
    {
        var contact = _dbContext.Contacts
            .FirstOrDefault(c => c.Id == id);

        if (contact is null)
        {
            return NotFound();
        }

        var contactDto = new ContactDto()
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Email = contact.Email
        };

        return Ok(contactDto);
    }

    // POST api/contacts
    [HttpPost]
    public IActionResult CreateContact([FromBody] ContactForCreationDto contactForCreationDto)
    {
        if (contactForCreationDto.FirstName == contactForCreationDto.LastName)
        {
            ModelState.AddModelError("wrongName", "First name and last name cannot be the same.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var contact = new Contact()
        {
            FirstName = contactForCreationDto.FirstName,
            LastName = contactForCreationDto.LastName,
            Email = contactForCreationDto.Email
        };

        _dbContext.Contacts.Add(contact);
        _dbContext.SaveChanges();

        var contactDto = new ContactDto()
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Email = contact.Email
        };

        return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contactDto);
    }

    // PUT api/contacts/1
    [HttpPut("{id:int}")]
    public IActionResult UpdateContact(int id, [FromBody] ContactForUpdateDto contactForUpdateDto)
    {
        var contact = _dataService
            .Contacts
            .FirstOrDefault(c => c.Id == id);

        if (contact is null)
        {
            return NotFound();
        }

        contact.FirstName = contactForUpdateDto.FirstName;
        contact.LastName = contactForUpdateDto.LastName;
        contact.Email = contactForUpdateDto.Email;

        return NoContent();
    }

    // DELETE api/contacts/1
    [HttpDelete("{id:int}")]
    public IActionResult DeleteContact()
    {
        var contact = _dataService
            .Contacts
            .FirstOrDefault(c => c.Id == 1);

        if (contact is null)
        {
            return NotFound();
        }

        _dataService.Contacts.Remove(contact);

        return NoContent();
    }

    // PATCH api/contacts/1
    [HttpPatch("{id:int}")]
    public IActionResult PartiallyUpdateContact(int id, [FromBody] JsonPatchDocument<ContactForUpdateDto> patchDocument)
    {
        var contact = _dataService
            .Contacts
            .FirstOrDefault(c => c.Id == id);

        if (contact is null)
        {
            return NotFound();
        }

        var contactToBePatched = new ContactForUpdateDto()
        {
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Email = contact.Email
        };

        patchDocument.ApplyTo(contactToBePatched, ModelState);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryValidateModel(contactToBePatched))
        {
            return BadRequest(ModelState);
        }

        contact.FirstName = contactToBePatched.FirstName;
        contact.LastName = contactToBePatched.LastName;
        contact.Email = contactToBePatched.Email;

        return NoContent();
    }
}