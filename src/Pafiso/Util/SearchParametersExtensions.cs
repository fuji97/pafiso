namespace Pafiso.Util;

public static class SearchParametersExtensions {
    public static IQueryable<T> Search<T>(this IQueryable<T> query, SearchParameters parameters) {
        return parameters.ApplyToIQueryable(query);
    }
    
    public static IEnumerable<T> Search<T>(this IEnumerable<T> query, SearchParameters parameters) {
        return parameters.ApplyToIQueryable(query.AsQueryable());
    }
}