using ElectionService.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ElectionService.Api.Middleware;

public sealed class ElectionExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ElectionValidationException exception)
        {
            var status = exception switch
            {
                ElectionNotFoundException => StatusCodes.Status404NotFound,
                ElectionConflictException => StatusCodes.Status409Conflict,
                ElectionForbiddenException => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = "Election request failed",
                Detail = exception.Message
            });
        }
    }
}