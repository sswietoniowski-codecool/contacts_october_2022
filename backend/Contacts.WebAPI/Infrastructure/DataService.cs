﻿using Contacts.WebAPI.Domain;

namespace Contacts.WebAPI.Infrastructure;

public class DataService
{
    public List<Contact> Contacts { get; }

    public DataService()
    {
        Contacts = new List<Contact>()
        {
            new Contact() {Id = 1, FirstName = "Jan", LastName = "Kowalski", Email = "jkowalski@u.pl"},
            new Contact() {Id = 2, FirstName = "Adam", LastName = "Nowak", Email = "anowak@u.pl"}
        };
    }
}