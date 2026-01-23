using System.Collections;

namespace Pafiso.Enumerables;

public class PagedEnumerable<T>(IEnumerable<T> countQuery, IEnumerable<T> entriesQuery) : IEnumerable<T> {
    private IEnumerable<T> CountQuery { get; init; } = countQuery;
    private IEnumerable<T> EntriesQuery { get; init; } = entriesQuery;

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
}

public static class PagedEnumerableExtensions {
    public static PagedEnumerable<T> WithSearchParameters<T>(this IEnumerable<T> query, SearchParameters searchParameters,
        Func<IEnumerable<T>, IEnumerable<T>>? applyQuery = null) {
        var entriesQuery = applyQuery?.Invoke(query) ?? query;

        return new PagedEnumerable<T>(query, entriesQuery);
    }
}
