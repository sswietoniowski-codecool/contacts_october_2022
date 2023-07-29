using AutoMapper;
using Contacts.WebAPI.Configurations.Options;
using Contacts.WebAPI.Domain;
using Contacts.WebAPI.DTOs;
using Contacts.WebAPI.Infrastructure.Repositories;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Contacts.WebAPI.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly IContactsRepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ContactsController> _logger;
    private readonly CorsConfiguration _corsConfiguration;

    public ContactsController(IContactsRepository repository, IMapper mapper, IMemoryCache memoryCache, ILogger<ContactsController> logger, IOptions<CorsConfiguration> corsOptions)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _corsConfiguration = corsOptions.Value ?? throw new ArgumentNullException(nameof(corsOptions));
    }

    // GET api/contacts?search=ski
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<IEnumerable<ContactDto>> GetContacts([FromQuery] string? search)
    {
        var origins = _corsConfiguration.Origins;

        if (origins.Contains(Request.Headers["Origin"].ToString()))
        {
            _logger.LogInformation($"Request from {Request.Headers["Origin"]} is allowed.");
        }
        else
        {
            _logger.LogWarning($"Request from {Request.Headers["Origin"]} is not allowed.");
        }

        var contacts = _repository.GetContacts(search);

        var contactsDto = _mapper.Map<IEnumerable<ContactDto>>(contacts);

        return Ok(contactsDto);
    }

    // GET api/contacts/1
    // GET api/contacts/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    //[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
    [ResponseCache(CacheProfileName = "Any-60")]
    public ActionResult<ContactDetailsDto> GetContact(int id)
    {
        _logger.LogInformation($"Getting contact with id {id}.");

        var cacheKey = $"{nameof(ContactsController)}-{nameof(GetContact)}-{id}";

        if (!_memoryCache.TryGetValue<ContactDetailsDto>(cacheKey, out var contactDto))
        {
            _logger.LogWarning($"Contact with id {id} was not found in cache. Retrieving from database.");

            var contact = _repository.GetContact(id);

            if (contact is not null)
            {
                contactDto = _mapper.Map<ContactDetailsDto>(contact);

                _memoryCache.Set(cacheKey, contactDto, TimeSpan.FromSeconds(60));
            }
        }

        if (contactDto is null)
        {
            _logger.LogError($"Contact with id {id} was not found in database.");

            return NotFound();
        }

        return Ok(contactDto);
    }

    // POST api/contacts
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        var contact = _mapper.Map<Contact>(contactForCreationDto);

        _repository.CreateContact(contact);

        var contactDto = _mapper.Map<ContactDto>(contact);

        return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contactDto);
    }

    // PUT api/contacts/1
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult UpdateContact(int id, [FromBody] ContactForUpdateDto contactForUpdateDto)
    {
        var contact = _mapper.Map<Contact>(contactForUpdateDto);
        contact.Id = id;

        var success = _repository.UpdateContact(contact);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    // DELETE api/contacts/1
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteContact(int id)
    {
        var success = _repository.DeleteContact(id);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    // PATCH api/contacts/1
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult PartiallyUpdateContact(int id, [FromBody] JsonPatchDocument<ContactForUpdateDto> patchDocument)
    {
        var contact = _repository.GetContact(id);

        if (contact is null)
        {
            return NotFound();
        }

        var contactToBePatched = _mapper.Map<ContactForUpdateDto>(contact);

        patchDocument.ApplyTo(contactToBePatched, ModelState);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!TryValidateModel(contactToBePatched))
        {
            return BadRequest(ModelState);
        }

        _mapper.Map(contactToBePatched, contact);

        var success = _repository.UpdateContact(contact);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}