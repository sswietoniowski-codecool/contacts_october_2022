using Contacts.WebAPI.Infrastructure.Repositories;
using Contacts.WebAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Contacts.WebAPI.Configurations.Options;

namespace Contacts.WebAPI.Configurations.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddPersistence(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ContactsDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("ContactsDb"));
            options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
        });

        builder.Services.AddScoped<IContactsRepository, ContactsRepository>();

        return builder;
    }

    public static WebApplicationBuilder AddCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<CorsConfiguration>()
            .Bind(builder.Configuration.GetSection("Cors"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policyBuilder =>
            {
                var origins = new List<string>();

                builder.Configuration.Bind("Cors:Origins", origins);

                policyBuilder
                    .WithOrigins(origins.ToArray())
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return builder;
    }
}