using System.Net;
using Contacts.WebAPI.Infrastructure;
using Contacts.WebAPI.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;
using Contacts.WebAPI.Configurations.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// add logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Add services to the container.

builder.Services.AddDbContext<ContactsDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ContactsDb"));
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

builder.Services.AddScoped<IContactsRepository, ContactsRepository>();

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

builder.Services.Configure<CorsConfiguration>(builder.Configuration.GetSection("Cors"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        //var origins = builder.Configuration.GetSection("Cors:Origins")
        //    .Get<string[]>()!;

        var origins = new List<string>();

        builder.Configuration.Bind("Cors:Origins", origins);

        policyBuilder
            .WithOrigins(origins.ToArray())
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers(configure =>
{
    configure.CacheProfiles.Add("Any-60",
        new CacheProfile
        {
            Location = ResponseCacheLocation.Any,
            Duration = 60
        });
    configure.CacheProfiles.Add("NoCache", new CacheProfile { NoStore = true });
}).AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

builder.Services.AddProblemDetails();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    configuration.ReadFrom.Services(services);
}, preserveStaticLogger: true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // should be added first
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // should be added first
    app.UseExceptionHandler(applicationBuilder =>
    {
        applicationBuilder.Run(async context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var problemDetails = new ProblemDetails
            {
                Title = "An unexpected error occurred!",
                Status = context.Response.StatusCode,
                Detail = "Please contact your system administrator!"
            };

            var problemDetailsJson = JsonSerializer.Serialize(problemDetails);

            // TODO: log the exception

            //await context.Response.WriteAsync("An unexpected fault happened. Try again later.");
            await context.Response.WriteAsync(problemDetailsJson);
        });
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors();

app.UseResponseCaching();

app.MapControllers();

app.Run();
