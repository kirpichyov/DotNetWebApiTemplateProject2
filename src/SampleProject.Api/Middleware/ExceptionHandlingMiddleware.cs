using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SampleProject.Core.Exceptions;
using SampleProject.Core.Models.Api;

namespace SampleProject.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = exception switch
        {
            ResourceNotFoundException => StatusCodes.Status404NotFound,
            ValidationFailedException => StatusCodes.Status400BadRequest,
            ConflictException => StatusCodes.Status409Conflict,
            InsufficientRightsException => StatusCodes.Status403Forbidden,
            ValidationException => StatusCodes.Status400BadRequest,
            DbUpdateConcurrencyException dbException => LogConcurrencyAndReturnCode(dbException),
            _ => LogUnexpectedAndReturn500(exception)
        };
        
        var payload = BuildResponseModel(exception, code, context);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = code;

        await context.Response.WriteAsJsonAsync(payload);
    }

    private int LogConcurrencyAndReturnCode(DbUpdateConcurrencyException dbException)
    {
        _logger.LogWarning(dbException, "Concurrency exception occurred during request");
        return StatusCodes.Status409Conflict;
    }

    private int LogUnexpectedAndReturn500(Exception exception)
    {
        _logger.LogError(exception, "Unexpected error occurred during request");
        return StatusCodes.Status500InternalServerError;
    }

    private ApiErrorResponse BuildResponseModel(Exception exception, int statusCode, HttpContext context)
    {
        ApiErrorResponse responseModel;

        switch (exception)
        {
            case CoreException coreException:
            {
                var nodes = coreException.PropertyErrors
                    .Select(pe => new ApiErrorResponseNode(pe.Property, pe.Errors))
                    .ToArray();

                responseModel = new ApiErrorResponse(coreException.Identifier, nodes)
                    .AddDetails(coreException.Extensions);
                break;
            }
            case ValidationException validationException:
            {
                var nodes = validationException.Errors
                    .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                    .Select(g => new ApiErrorResponseNode(g.Key, g.ToArray()))
                    .ToArray();

                responseModel = new ApiErrorResponse(ExceptionsInfo.Identifiers.ModelValidationFailed, nodes);
                break;
            }
            default:
                responseModel = new ApiErrorResponse(
                    ExceptionsInfo.Identifiers.Generic,
                    new ApiErrorResponseNode(null, "Unexpected error occurred.")
                );
                break;
        }

        if (!_environment.IsProduction())
        {
            responseModel = new ApiErrorResponseWithException(responseModel, exception);
        }

        if (statusCode >= 500 &&
            context.Response.Headers.TryGetValue("X-Correlation-ID", out var id))
        {
            responseModel
                .AddDetail("correlationId", id.FirstOrDefault()!)
                .AddDetail("tip", $"Please send this code {id} to support or try again later.");
        }

        return responseModel;
    }
}