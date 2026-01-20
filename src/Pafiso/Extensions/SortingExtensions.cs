namespace Pafiso.Extensions;

public static class SortingExtensions {
    public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> query, Sorting sorting) {
        return sorting.ApplyToIQueryable(query);
    }
    
    public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> query, Sorting sorting) {
        return sorting.ApplyToIQueryable(query.AsQueryable()).ToList();
    }
    
    public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> query, Sorting sorting) {
        return sorting.ThenApplyToIQueryable(query);
    }
}