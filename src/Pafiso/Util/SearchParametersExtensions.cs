using Pafiso.Enumerables;

namespace Pafiso.Util;

public static class SearchParametersExtensions {
    public static PagedQueryable<T> Search<T>(this IQueryable<T> query, SearchParameters parameters) {
        return parameters.ApplyToIQueryable(query);
    }
    
    public static PagedEnumerable<T> Search<T>(this IEnumerable<T> query, SearchParameters parameters) {
        return parameters.ApplyToIQueryable(query.AsQueryable());
    }
}