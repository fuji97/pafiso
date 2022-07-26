namespace Pafiso.Util;

public static class PagingExtensions {
    public static IQueryable<T> Paging<T>(this IQueryable<T> query, Paging paging) {
        return paging.ApplyToIQueryable(query);
    }
    
    public static IEnumerable<T> Paging<T>(this IEnumerable<T> query, Paging paging) {
        return paging.ApplyToIQueryable(query.AsQueryable());
    }
}