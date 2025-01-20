using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Trelnex.Core.Api.Responses;

namespace Trelnex.Core.Api.Exceptions;

/// <summary>
/// A class for handling <see cref="HttpStatusCodeException"/>.
/// </summary>
public class HttpStatusCodeExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not HttpStatusCodeException httpStatusCodeException) return false;

        var httpStatusCodeResponse = new HttpStatusCodeResponse
        {
            StatusCode = (int)httpStatusCodeException.HttpStatusCode,
            Message = httpStatusCodeException.Message,
            Errors = httpStatusCodeException.Errors,
        };

        httpContext.Response.StatusCode = httpStatusCodeResponse.StatusCode;

        await httpContext.Response.WriteAsJsonAsync(httpStatusCodeResponse, _options, cancellationToken);

        return true;
    }
}
