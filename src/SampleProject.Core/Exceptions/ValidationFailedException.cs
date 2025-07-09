namespace SampleProject.Core.Exceptions;

public sealed class ValidationFailedException : CoreException
{
    public ValidationFailedException(string message, Dictionary<string, string> extensions = null)
        : base([new PropertyErrors(null, message)], ExceptionsInfo.Identifiers.ValidationFailed, extensions)
    {
    }

    public ValidationFailedException(string propertyName, string propertyError, Dictionary<string, string> extensions = null)
        : base([new PropertyErrors(propertyName, propertyError)], ExceptionsInfo.Identifiers.ValidationFailed, extensions)
    {
    }
}