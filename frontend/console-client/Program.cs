// CRUD

using System.Text;
using System.Text.Json;
using Contacts.Client.DTOs;

var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://localhost:5001");

Console.WriteLine("CreateContact:\n");

var contactForCreationDto = new ContactForCreationDto
{
    FirstName = "John",
    LastName = "Doe",
    Email = "jdoe@unknown.com"
};

var contactForCreationDtoJson = JsonSerializer.Serialize(contactForCreationDto);

var request = new HttpRequestMessage(HttpMethod.Post, "api/contacts");
request.Content = new StringContent(contactForCreationDtoJson, Encoding.UTF8, "application/json");

var response = await httpClient.SendAsync(request);

response.EnsureSuccessStatusCode();

var createdContactJson = await response.Content.ReadAsStringAsync();

var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
var createdContact = JsonSerializer.Deserialize<ContactDto>(createdContactJson, jsonSerializerOptions);

Console.WriteLine($"{createdContact.Id} {createdContact.FirstName} {createdContact.LastName}");