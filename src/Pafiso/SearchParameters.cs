using Pafiso.Enumerables;
using Pafiso.Extensions;
using Pafiso.Util;

namespace Pafiso;

[Serializable]
public class SearchParameters {
    private Paging? _paging = null;
    private List<Sorting> _sortings = [];
    private List<Filter> _filters = [];

    public Paging? Paging {
        get => _paging;
        set => _paging = value;
    }
    public List<Sorting> Sortings {
        get => _sortings;
        set => _sortings = value;
    }
    public List<Filter> Filters {
        get => _filters;
        set => _filters = value;
    }

    public SearchParameters() {
    }

    public SearchParameters(Paging? paging) {
        _paging = paging;
    }

    public SearchParameters AddSorting(params Sorting[] sorting) {
        Sortings.AddRange(sorting);
        return this;
    }

    public SearchParameters AddFilters(params Filter[] filters) {
        Filters.AddRange(filters);
        return this;
    }

    public (IQueryable<T> countQuery, IQueryable<T> pagedQuery) ApplyToIQueryable<T>(IQueryable<T> query) {
        return ApplyToIQueryableInternal<T>(query, null);
    }

    /// <summary>
    /// Applies search parameters to the queryable with field-level restrictions.
    /// Restricted fields are silently ignored.
    /// </summary>
    /// <param name="query">The source queryable to apply parameters to.</param>
    /// <param name="configureRestrictions">Action to configure field restrictions using a fluent builder.</param>
    /// <returns>A tuple containing the count query (before paging) and the paged query.</returns>
    public (IQueryable<T> countQuery, IQueryable<T> pagedQuery) ApplyToIQueryable<T>(
        IQueryable<T> query,
        Action<FieldRestrictions> configureRestrictions) {
        var restrictions = new FieldRestrictions();
        configureRestrictions(restrictions);
        return ApplyToIQueryableInternal(query, restrictions);
    }

    private (IQueryable<T> countQuery, IQueryable<T> pagedQuery) ApplyToIQueryableInternal<T>(
        IQueryable<T> query,
        FieldRestrictions? restrictions) {

        if (Filters.Count != 0) {
            IEnumerable<Filter> filtersToApply;
            if (restrictions == null) {
                filtersToApply = Filters;
            } else {
                // For each filter, get only the allowed fields
                // If no fields are allowed, skip the filter entirely
                filtersToApply = Filters
                    .Select(f => {
                        var allowedFields = restrictions.GetAllowedFilterFields(f);
                        if (allowedFields.Count == 0) return null;
                        if (allowedFields.Count == f.Fields.Count) return f;
                        return new Filter(allowedFields, f.Operator, f.Value, f.CaseSensitive);
                    })
                    .OfType<Filter>();
            }
            query = filtersToApply.Aggregate(query, (current, filter) => current.Where(filter));
        }

        if (Sortings.Count != 0) {
            var distinctSortings = Sortings.DistinctBy(x => x.PropertyName).ToArray();
            var sortingsToApply = restrictions == null
                ? distinctSortings
                : distinctSortings.Where(s => restrictions.IsSortFieldAllowed(s.PropertyName)).ToArray();

            if (sortingsToApply.Length > 0) {
                var orderedQuery = query.OrderBy(sortingsToApply.First());
                query = sortingsToApply.Skip(1).Aggregate(orderedQuery,
                    (current, sorting) => current.ThenBy(sorting));
            }
        }

        var countQuery = query;

        if (Paging != null) query = query.Paging(Paging);

        return (countQuery, query);
    }

    public IDictionary<string, string> ToDictionary() {
        var dicts = new List<IDictionary<string, string>>() {
            QueryStringHelpers.MergeListOfQueryStrings("sortings",
                Sortings.DistinctBy(x => x.PropertyName).Select(s => s.ToDictionary())),    // Remove duplicates
            QueryStringHelpers.MergeListOfQueryStrings("filters",
                Filters.Select(f => f.ToDictionary())),
            Paging?.ToDictionary() ?? new Dictionary<string, string>()
        };

        return dicts.SelectMany(dict => dict)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public static SearchParameters FromDictionary(IDictionary<string, string> dict) {
        var splitted = QueryStringHelpers.SplitQueryStringInList(dict);

        var paging = Paging.FromDictionary(dict);
        var sorting = splitted.ContainsKey("sortings") ? splitted["sortings"].Select(Sorting.FromDictionary) : null;
        var filters = splitted.ContainsKey("filters") ? splitted["filters"].Select(Filter.FromDictionary) : null;
        return new SearchParameters {
            Paging = paging,
            Sortings = sorting?.ToList() ?? [],
            Filters = filters?.ToList() ?? []
        };
    }

    protected bool Equals(SearchParameters other) {
        return Nullable.Equals(Paging, other.Paging) && Sortings.SequenceEqual(other.Sortings) &&
               Filters.SequenceEqual(other.Filters);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SearchParameters)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Paging, Sortings, Filters);
    }

    public static bool operator ==(SearchParameters? left, SearchParameters? right) {
        return Equals(left, right);
    }

    public static bool operator !=(SearchParameters? left, SearchParameters? right) {
        return !Equals(left, right);
    }

    public static SearchParameters operator +(SearchParameters left, SearchParameters right) {
        return new SearchParameters {
            Paging = left.Paging ?? right.Paging,
            Sortings = left.Sortings.Concat(right.Sortings).ToList(),
            Filters = left.Filters.Concat(right.Filters).ToList()
        };
    }

    public override string ToString() {
        return $"Paging: {Paging?.ToString() ?? "---"}; " +
               $"Sortings: {string.Join(" -> ", Sortings.Select(x => x.ToString()))}; " +
               $"Filters: {string.Join(" AND ", Filters.Select(x => x.ToString()))}";
    }
}