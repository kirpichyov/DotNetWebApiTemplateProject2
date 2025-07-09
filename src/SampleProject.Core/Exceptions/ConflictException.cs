namespace SampleProject.Core.Exceptions;

public sealed class ConflictException : CoreException
{
    public ConflictException(string message, Dictionary<string, string> extensions = null)
        : base([new PropertyErrors(null, message)], ExceptionsInfo.Identifiers.Conflict, extensions)
    {
    }

    public ConflictException(IEnumerable<PropertyErrors> propertyErrors, Dictionary<string, string> extensions = null)
        : base(propertyErrors, ExceptionsInfo.Identifiers.Conflict, extensions)
    {
    }
}