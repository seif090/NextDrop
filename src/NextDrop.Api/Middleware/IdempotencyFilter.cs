using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextDrop.Infrastructure.Services;
using NextDrop.SharedKernel.Abstractions;

namespace NextDrop.Api.Middleware;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var keyHeader) || string.IsNullOrWhiteSpace(keyHeader))
        {
            await next();
            return;
        }

        var idempotencyKey = keyHeader.ToString();
        var idempotencyService = httpContext.RequestServices.GetRequiredService<IIdempotencyService>();

        httpContext.Request.EnableBuffering();
        httpContext.Request.Body.Position = 0;

        string bodyString;
        using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
        {
            bodyString = await reader.ReadToEndAsync();
            httpContext.Request.Body.Position = 0;
        }

        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(bodyString))).ToLowerInvariant();

        if (idempotencyService is InMemoryIdempotencyService inMemService && inMemService.IsPayloadMismatch(idempotencyKey, requestHash))
        {
            var correlationId = httpContext.Items[CorrelationIdMiddleware.CorrelationIdHeaderName]?.ToString() ?? Guid.NewGuid().ToString();
            var conflictDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Idempotency Conflict",
                Detail = "The provided Idempotency-Key has already been used with a different request payload.",
                Instance = httpContext.Request.Path
            };
            conflictDetails.Extensions["correlationId"] = correlationId;

            context.Result = new ObjectResult(conflictDetails)
            {
                StatusCode = StatusCodes.Status409Conflict,
                ContentTypes = { "application/problem+json" }
            };
            return;
        }

        var cachedResponse = await idempotencyService.GetCachedResponseAsync(idempotencyKey);
        if (cachedResponse != null)
        {
            context.Result = new ContentResult
            {
                StatusCode = cachedResponse.StatusCode,
                ContentType = cachedResponse.ContentType,
                Content = cachedResponse.Body
            };
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult && objectResult.StatusCode is >= 200 and < 300)
        {
            var responseBody = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
            await idempotencyService.CacheResponseAsync(
                idempotencyKey,
                requestHash,
                objectResult.StatusCode.Value,
                "application/json",
                responseBody);
        }
    }
}
