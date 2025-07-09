namespace SampleProject.Application.Utils;

public sealed class DetailsBuilder
{
    private readonly Dictionary<string, string> _errors = new();
    
    public DetailsBuilder Add(string key, string value)
    {
        _errors.TryAdd(key, value);
        return this;
    }
    
    public DetailsBuilder Add(string key, IEnumerable<string> value)
    {
        _errors.TryAdd(key, string.Join(", ", value));
        return this;
    }
    
    public Dictionary<string, string> Build()
    {
        return _errors;
    }
}