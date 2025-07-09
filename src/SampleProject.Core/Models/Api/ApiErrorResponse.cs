namespace SampleProject.Core.Models.Api;

public class ApiErrorResponse
{
    public ApiErrorResponse(string errorType, ApiErrorResponseNode[] errorNodes)
    {
        ErrorType = errorType;
        Errors = errorNodes;
        Details = [];
    }

    public ApiErrorResponse(string errorType, ApiErrorResponseNode errorNode)
    {
        ErrorType = errorType;
        Errors = [errorNode];
        Details = [];
    }

    public ApiErrorResponse AddDetail(string key, string value)
    {
        if (Details is null)
        {
            Details = new Dictionary<string, string>()
            {
                { key, value }
            };
            
            return this;
        }
        
        Details.TryAdd(key, value);
        return this;
    }
    
    public ApiErrorResponse AddDetails(Dictionary<string, string> details)
    {
        if (details is null)
        {
            Details = [];
            return this;
        }
        
        foreach (var (key, value) in details)
        {
            Details.TryAdd(key, value);
        }
        
        return this;
    }
    
    public string ErrorType { get; init; }
    public ApiErrorResponseNode[] Errors { get; init; }
    public Dictionary<string, string> Details { get; private set; }
}