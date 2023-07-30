﻿using Contacts.WebAPI.Domain;
using Microsoft.EntityFrameworkCore;

namespace Contacts.WebAPI.Infrastructure.Repositories;

public class ContactsRepository : IContactsRepository
{
    private readonly ContactsDbContext _dbContext;

    public ContactsRepository(ContactsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IEnumerable<Contact>> GetContactsAsync(string? search)
    {
        var query = _dbContext.Contacts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.LastName.Contains(search));
        }

        return await query.ToListAsync();
    }

    public async Task<Contact?> GetContactAsync(int id)
    {
        return await _dbContext.Contacts.Include(c => c.Phones)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task CreateContactAsync(Contact contact)
    {
        _dbContext.Contacts.Add(contact);
        await _dbContext.SaveChangesAsync();
    }

    public bool UpdateContact(Contact contact)
    {
        var contactFromDb = _dbContext
            .Contacts
            .FirstOrDefault(c => c.Id == contact.Id);

        if (contactFromDb is null)
        {
            return false;
        }

        contactFromDb.FirstName = contact.FirstName;
        contactFromDb.LastName = contact.LastName;
        contactFromDb.Email = contact.Email;

        _dbContext.SaveChanges();

        return true;
    }

    public bool DeleteContact(int id)
    {
        var contact = _dbContext
            .Contacts
            .FirstOrDefault(c => c.Id == id);

        if (contact is null)
        {
            return false;
        }

        _dbContext.Contacts.Remove(contact);
        _dbContext.SaveChanges();

        return true;
    }
}