using Contacts.WebAPI.Configurations.Extensions;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// add logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Add services to the container.

builder.AddPersistence();

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

builder.AddCors();

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

// should be added first
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors();

app.UseResponseCaching();

app.MapControllers();

app.Run();
