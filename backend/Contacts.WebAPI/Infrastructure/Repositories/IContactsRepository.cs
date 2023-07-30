﻿using Contacts.WebAPI.Domain;

namespace Contacts.WebAPI.Infrastructure.Repositories;

public interface IContactsRepository
{
    Task<IEnumerable<Contact>> GetContactsAsync(string? search);
    Task<Contact?> GetContactAsync(int id);
    void CreateContact(Contact contact);
    bool UpdateContact(Contact contact);
    bool DeleteContact(int id);
}