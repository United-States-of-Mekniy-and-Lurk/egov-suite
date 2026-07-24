using Microsoft.AspNetCore.Mvc;
using OrganizationRegistry.Application.Exceptions;

namespace OrganizationRegistry.Api.Services;

public sealed class RegistryExceptionMiddleware(RequestDelegate next, ILogger<RegistryExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (exception is
            RegistryValidationException or RegistryNotFoundException or RegistryForbiddenException or RegistryConflictException)
        {
            var status = exception switch
            {
                RegistryValidationException => StatusCodes.Status400BadRequest,
                RegistryNotFoundException => StatusCodes.Status404NotFound,
                RegistryForbiddenException => StatusCodes.Status403Forbidden,
                RegistryConflictException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };
            logger.LogInformation(exception, "Registry request failed with status {StatusCode}", status);
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = exception.Message
            });
        }
    }
}