using SampleProject.Core.Models.Dtos.Common;

namespace SampleProject.Application.Models.Common;

public class PagingResponse<TItem>
{
    public PagingResponse()
    {
    }

    protected PagingResponse(PageMetadataModel meta, IEnumerable<TItem> items)
    {
        Meta = meta;
        Items = items.ToArray();
    }
    
    public IReadOnlyCollection<TItem> Items { get; init; }
    public PageMetadataModel Meta { get; init; }

    public static PagingResponse<TItem> CreateEmpty()
    {
        return new PagingResponse<TItem>(new PageMetadataModel(0, 1, 0), []);
    }
}

public static class PagingResponse
{
    public static PagingResponse<TMappedItem> CreateFromPage<TSourceItem, TMappedItem>(
        Page<TSourceItem> page,
        IEnumerable<TMappedItem> items)
    {
        return new PagingResponse<TMappedItem>()
        {
            Items = items.ToArray(),
            Meta = new PageMetadataModel(page.PageCount, page.PageNumber, page.ItemsCount),
        };
    }
}