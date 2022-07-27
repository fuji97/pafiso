using Pafiso.Enumerables;

namespace Pafiso.Util;

public static class PagedEnumerableExtensions {
    public static PagedEnumerable<T> ToPagedEnumerable<T>(this IQueryable<T> query, IQueryable<T> source) {
        return new PagedEnumerable<T>(source.Count(), query.ToList());
    }
    
    public static PagedEnumerable<T> ToPagedEnumerable<T>(this IQueryable<T> query, int count) {
        return new PagedEnumerable<T>(count, query.ToList());
    }
    
    public static PagedEnumerable<T> ToPagedEnumerable<T>(this IQueryable<T> query) {
        return new PagedEnumerable<T>(query.Count(), query.ToList());
    }
    
    public static PagedEnumerable<T> ToPagedEnumerable<T>(this IEnumerable<T> query, IEnumerable<T> source) {
        return new PagedEnumerable<T>(source.Count(), query.ToList());
    }
    
    public static PagedEnumerable<T> ToPagedEnumerable<T>(this IEnumerable<T> query, int count) {
        return new PagedEnumerable<T>(count, query.ToList());
    }
    
    public static PagedEnumerable<T> ToPagedEnumerable<T>(this IEnumerable<T> query) {
        return new PagedEnumerable<T>(query.Count(), query.ToList());
    }
}