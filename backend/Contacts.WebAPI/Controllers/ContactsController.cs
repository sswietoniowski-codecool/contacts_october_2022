using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using Contacts.WebAPI.Configurations.Options;
using Contacts.WebAPI.Domain;
using Contacts.WebAPI.DTOs;
using Contacts.WebAPI.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Contacts.WebAPI.Controllers;

[ApiController]
[Route("api/contacts")]
[Authorize]
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
    // GET api/contacts?lastName=Nowak
    // GET api/contacts?orderBy=lastName
    // GET api/contacts?orderBy=lastName&desc=true
    // GET api/contacts?orderBy=lastName&desc=true&pageNumber=2&pageSize=2
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ContactDto>>> GetContacts(
        [FromQuery] string? search,
        [FromQuery] string? lastName,
        [FromQuery] string? orderBy,
        [FromQuery] bool? desc,
        [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
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

        if (pageNumber is null || pageNumber <= 0)
        {
            pageNumber = 1;
        }

        if (pageSize is null || pageSize <= 0 || pageSize > 10)
        {
            pageSize = 2;
        }

        var (contacts, paginationMetadata) = await _repository.GetContactsAsync(search, lastName, orderBy, desc, (int)pageNumber, (int)pageSize);

        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));

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
    public async Task<ActionResult<ContactDetailsDto>> GetContact(int id)
    {
        _logger.LogInformation($"Getting contact with id {id}.");

        var cacheKey = $"{nameof(ContactsController)}-{nameof(GetContact)}-{id}";

        if (!_memoryCache.TryGetValue<ContactDetailsDto>(cacheKey, out var contactDto))
        {
            _logger.LogWarning($"Contact with id {id} was not found in cache. Retrieving from database.");

            var contact = await _repository.GetContactAsync(id);

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
    public async Task<IActionResult> CreateContact([FromBody] ContactForCreationDto contactForCreationDto)
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

        await _repository.CreateContactAsync(contact);

        var contactDto = _mapper.Map<ContactDto>(contact);

        return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contactDto);
    }

    // PUT api/contacts/1
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateContact(int id, [FromBody] ContactForUpdateDto contactForUpdateDto)
    {
        var contact = _mapper.Map<Contact>(contactForUpdateDto);
        contact.Id = id;

        // get the user id from the claims & save it to the contact
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        contact.UserId = userIdClaim!;

        var success = await _repository.UpdateContactAsync(contact);

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
    //[Authorize(Roles = "Admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteContact(int id)
    {
        var success = await _repository.DeleteContactAsync(id);

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
    public async Task<IActionResult> PartiallyUpdateContact(int id, [FromBody] JsonPatchDocument<ContactForUpdateDto> patchDocument)
    {
        var contact = await _repository.GetContactAsync(id);

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

        var success = await _repository.UpdateContactAsync(contact);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}