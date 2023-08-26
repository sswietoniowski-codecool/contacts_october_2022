using Contacts.WebAPI.Configurations.Extensions;
using Contacts.WebAPI.Domain;
using Contacts.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Identity;
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
    //configure.Filters.Add(new AuthorizeFilter());
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

// add Identity service
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;

    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ContactsDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.Cookie.Name = "ContactApp.Cookies";

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.LoginPath = "/api/users/login-cookie";
        options.LogoutPath = "/api/users/logout-cookie";
        options.AccessDeniedPath = "/api/users/access-denied";
    });

builder.Services.AddAuthorization();

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

// seed database
app.InitializeDatabase();

app.Run();
