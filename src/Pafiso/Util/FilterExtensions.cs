using System.Linq.Expressions;
using LinqKit;

namespace Pafiso.Util;

public static class FilterExtensions {
    public static IQueryable<T> Where<T>(this IQueryable<T> query, Filter filter) {
        return filter.ApplyFilter(query);
    }
    
    public static IEnumerable<T> Where<T>(this IEnumerable<T> query, Filter filter) {
        return filter.ApplyFilter(query.AsQueryable());
    }
}