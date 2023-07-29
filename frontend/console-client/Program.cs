// CRUD

using System.Text.Json;
using Contacts.Client.DTOs;

var httpClient = new HttpClient();

Console.WriteLine("GetContacts:\n");

var response = await httpClient.GetAsync("https://localhost:5001/api/contacts");

response.EnsureSuccessStatusCode();

var content = await response.Content.ReadAsStringAsync();

var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
var contacts = JsonSerializer.Deserialize<List<ContactDto>>(content, jsonSerializerOptions);
contacts ??= new List<ContactDto>();

foreach (var contact in contacts)
{
    Console.WriteLine($"{contact.Id} {contact.FirstName} {contact.LastName} {contact.Email}");
}

Console.WriteLine("\nGetContact:\n");

var id = 1;
response = await httpClient.GetAsync($"https://localhost:5001/api/contacts/{id}");

response.EnsureSuccessStatusCode();

content = await response.Content.ReadAsStringAsync();

var contactDto = JsonSerializer.Deserialize<ContactDetailsDto>(content);

Console.WriteLine($"{contactDto.Id} {contactDto.FirstName} {contactDto.LastName} {contactDto.Email}");