using System.Linq.Expressions;
using SampleProject.Core.Models.Dtos.Common;

namespace SampleProject.DataAccess.DataManipulation;

public interface IExpressionFilter<TItem>
{
    Expression<Func<TItem, bool>> FilteringExpression { get; set; }
    Expression<Func<TItem, object>> OrderingExpression { get; set; }
    OrderingDirection OrderingDirection { get; set; }
    Task<Page<TItem>> ApplyToQueryable(IQueryable<TItem> source, Expression<Func<TItem, bool>> additionalFilter = null);
}