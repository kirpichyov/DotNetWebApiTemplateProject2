namespace SampleProject.Core.Exceptions;

public sealed class PropertyErrors
{
    public PropertyErrors(string property, string[] errors)
    {
        Property = property;
        Errors = errors;
    }

    public PropertyErrors(string property, string error)
    {
        Property = property;
        Errors = [error];
    }

    public string Property { get; init; }
    public string[] Errors { get; init; }
}