using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace OneGuard;

public sealed class CustomExceptionHandler : IExceptionHandler
{

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var response = httpContext.Response;
        response.ContentType = "application/json";
        string message;
        string clientMessage;
        switch (exception)
        {
            case CoreException coreException:
                response.StatusCode = coreException.Code;
                message = coreException.Message;
                clientMessage = coreException.ClientMessage;
                break;
            case ValidationException validationException:
                response.StatusCode = 422;
                message = string.Join(", ", validationException.Errors
                    .Select(failure => $"{failure.PropertyName} : {failure.ErrorMessage}"));
                clientMessage = "خطا در مقادیر ارسالی";
                break;
            default:
                response.StatusCode = 500;
                clientMessage = "خطای سیستمی";
                message = "Whoops :( , something impossibly went wrong!";
                break;
        }

        await response.WriteAsync(JsonSerializer.Serialize(new
        {
            message,
            clientMessage
        }), cancellationToken: cancellationToken);
        return true;
    }
}