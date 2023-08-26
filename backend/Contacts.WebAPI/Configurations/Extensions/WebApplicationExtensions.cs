using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Contacts.WebAPI.Configurations.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseErrorHandling(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
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

        return app;
    }
}