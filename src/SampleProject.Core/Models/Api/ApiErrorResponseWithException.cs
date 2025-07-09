namespace SampleProject.Core.Models.Api;

public sealed class ApiErrorResponseWithException : ApiErrorResponse
{
    public ApiErrorResponseWithException(ApiErrorResponse original, Exception exception)
        : base(original.ErrorType, original.Errors)
    {
        ExceptionMessage = exception.Message;
        AddDetails(original.Details);
    }
    
    public string ExceptionMessage { get; }
}