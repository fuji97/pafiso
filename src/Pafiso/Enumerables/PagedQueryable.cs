using System.Collections;
using System.Linq.Expressions;

namespace Pafiso.Enumerables;

public class PagedQueryable<T>(IQueryable<T> countQuery, IQueryable<T> entriesQuery) : IQueryable<T> {
    private IQueryable<T> CountQuery { get; init; } = countQuery;
    private IQueryable<T> EntriesQuery { get; init; } = entriesQuery;

    public PagedList<T> ToPagedList() {
        return new PagedList<T>() {
            TotalEntries = CountQuery.Count(),
            Entries = EntriesQuery.ToList()
        };
    }

    public IEnumerator<T> GetEnumerator() {
        return EntriesQuery.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)EntriesQuery).GetEnumerator();
    }

    public Type ElementType => EntriesQuery.ElementType;

    public Expression Expression => EntriesQuery.Expression;

    public IQueryProvider Provider => EntriesQuery.Provider;
}

public static class PagedQueryableExtensions {
    public static PagedQueryable<T> WithSearchParameters<T>(this IQueryable<T> query, SearchParameters searchParameters) {
        var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(query);
        return new PagedQueryable<T>(countQuery, pagedQuery);
    }

    public static PagedQueryable<T> WithSearchParameters<T>(
        this IQueryable<T> query,
        SearchParameters searchParameters,
        PafisoSettings? settings) {
        var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(query, settings);
        return new PagedQueryable<T>(countQuery, pagedQuery);
    }
}