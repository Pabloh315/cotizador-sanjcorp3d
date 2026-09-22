using Microsoft.AspNetCore.Diagnostics;

namespace SanjCorp3D.Api.Infrastructure;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ArgumentException and not FormatException)
        {
            return false;
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            message = exception.Message
        }, cancellationToken);
        return true;
    }
}
