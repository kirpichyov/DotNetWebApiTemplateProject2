namespace SampleProject.Core.Exceptions;

public sealed class ResourceNotFoundException : CoreException
{
    public ResourceNotFoundException(string itemName, Dictionary<string, string> extensions = null)
        : base([new PropertyErrors(null, $"{itemName} is not found.")], 
            ExceptionsInfo.Identifiers.ResourceNotFound,
            extensions)
    {
        ItemName = itemName;
    }
    
    public string ItemName { get; }
}