namespace SampleProject.Core.Extensions;

public static class CollectionExtensions
{
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(items);

        if (collection is List<T> list)
        {
            list.AddRange(items);
            return;
        }
        
        if (collection is HashSet<T> set)
        {
            foreach (var item in items)
            {
                set.Add(item);
            }
            return;
        }
        
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}