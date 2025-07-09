namespace SampleProject.Application.Models.Common;

public class CollectionModel<TItem>
{
    public CollectionModel(long itemsTotalCount, IEnumerable<TItem> items)
    {
        ItemsTotalCount = itemsTotalCount;
        Items = items.ToArray();
    }
    
    public CollectionModel(IEnumerable<TItem> items)
    {
        Items = items.ToArray();
        ItemsTotalCount = Items.Count;
    }

    public CollectionModel()
    {
    }
    
    public long ItemsTotalCount { get; init; }
    public IReadOnlyCollection<TItem> Items { get; init; }
}