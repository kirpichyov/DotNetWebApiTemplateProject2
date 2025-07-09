namespace SampleProject.Core.Exceptions;

public class CoreException : Exception
{
    protected CoreException(string message, string identifier, Dictionary<string, string> extensions = null)
        : base(message)
    {
        Identifier = identifier;
        PropertyErrors = [];
        Extensions = extensions;
    }

    protected CoreException(
        IEnumerable<PropertyErrors> propertyErrors,
        string identifier,
        Dictionary<string, string> extensions = null)
        : base("Validation failed.")
    {
        Identifier = identifier;
        PropertyErrors = propertyErrors.ToArray();
        Extensions = extensions;
    }

    public string Identifier { get; }
    public PropertyErrors[] PropertyErrors { get; }
    public Dictionary<string, string> Extensions { get; }
}