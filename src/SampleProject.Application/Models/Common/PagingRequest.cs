namespace SampleProject.Application.Models.Common;

public class PagingRequest
{
    public const int MaxPageSize = 200;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = MaxPageSize;
}