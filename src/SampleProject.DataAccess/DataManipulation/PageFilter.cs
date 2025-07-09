using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SampleProject.Core.Models.Dtos.Common;

namespace SampleProject.DataAccess.DataManipulation;

public class PageFilter<TItem> : IPageFilter<TItem>
{
    public Expression<Func<TItem, bool>> FilteringExpression { get; set; }
    public Expression<Func<TItem, object>> OrderingExpression { get; set; }
    public OrderingDirection OrderingDirection { get; set; }
    
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public async Task<Page<TItem>> ApplyToQueryable(IQueryable<TItem> source, Expression<Func<TItem, bool>> additionalFilter = null)
    {
        if (FilteringExpression is not null)
        {
            source = source.Where(FilteringExpression);
        }
        
        if (additionalFilter is not null)
        {
            source = source.Where(additionalFilter);
        }

        if (OrderingExpression is not null)
        {
            source = OrderingDirection is OrderingDirection.Ascending
                ? source.OrderBy(OrderingExpression)
                : source.OrderByDescending(OrderingExpression);
        }

        var skip = PageNumber < 1
            ? 0
            : (PageNumber - 1) * PageSize;

        var itemsCount = await source.CountAsync();
        var pagedItems = await source
            .Skip(skip)
            .Take(PageSize)
            .ToArrayAsync();

        return new Page<TItem>(pagedItems, itemsCount, PageSize, PageNumber);
    }
    
    public async Task<Page<TDto>> ApplyToQueryable<TDto>(
        IQueryable<TItem> source, 
        Expression<Func<TItem, TDto>> projection,
        Expression<Func<TItem, bool>> additionalFilter = null)
    {
        if (FilteringExpression is not null)
        {
            source = source.Where(FilteringExpression);
        }
        
        if (additionalFilter is not null)
        {
            source = source.Where(additionalFilter);
        }

        if (OrderingExpression is not null)
        {
            source = OrderingDirection is OrderingDirection.Ascending
                ? source.OrderBy(OrderingExpression)
                : source.OrderByDescending(OrderingExpression);
        }

        var skip = PageNumber < 1
            ? 0
            : (PageNumber - 1) * PageSize;

        var itemsCount = await source.CountAsync();
        var pagedItems = await source
            .Skip(skip)
            .Take(PageSize)
            .Select(projection)
            .ToArrayAsync();

        return new Page<TDto>(pagedItems, itemsCount, PageSize, PageNumber);
    }
}