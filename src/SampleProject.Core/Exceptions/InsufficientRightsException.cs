namespace SampleProject.Core.Exceptions;

public sealed class InsufficientRightsException : CoreException
{
    public InsufficientRightsException(string message, Dictionary<string, string> extensions = null)
        : base(message, ExceptionsInfo.Identifiers.ValidationFailed, extensions)
    {
    }

    public InsufficientRightsException(IEnumerable<PropertyErrors> propertyErrors, Dictionary<string, string> extensions = null)
        : base(propertyErrors, ExceptionsInfo.Identifiers.ValidationFailed, extensions)
    {
    }
}