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
    extension<T>(IEnumerable<T> query) {
        public PagedEnumerable<T> WithSearchParameters(SearchParameters searchParameters) {
            var queryable = query.AsQueryable();
            var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(queryable);
            return new PagedEnumerable<T>(countQuery, pagedQuery);
        }

        public PagedEnumerable<T> WithSearchParameters(SearchParameters searchParameters,
            PafisoSettings? settings) {
            var queryable = query.AsQueryable();
            var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(queryable, settings);
            return new PagedEnumerable<T>(countQuery, pagedQuery);
        }
    }
}
