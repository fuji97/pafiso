using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pafiso.Enumerables;

namespace Pafiso.EntityFrameworkCore.Enumerables;

public class PagedQueryable<T>(IQueryable<T> countQuery, IQueryable<T> entriesQuery) : IQueryable<T> {
    private IQueryable<T> CountQuery { get; init; } = countQuery;
    private IQueryable<T> EntriesQuery { get; init; } = entriesQuery;

    public async Task<PagedList<T>> ToPagedListAsync() {
        return new PagedList<T>() {
            TotalEntries = await CountQuery.CountAsync(),
            Entries = await EntriesQuery.ToListAsync()
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
    public static PagedQueryable<T> WithSearchParameters<T>(this IQueryable<T> query, SearchParameters searchParameters,
        Func<IQueryable<T>, IQueryable<T>>? applyQuery = null) {
        var entriesQuery = applyQuery?.Invoke(query) ?? query;

        return new PagedQueryable<T>(query, entriesQuery);
    }
}
