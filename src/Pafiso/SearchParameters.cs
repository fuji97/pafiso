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

    /// <summary>
    /// Applies search parameters to the queryable.
    /// </summary>
    /// <param name="query">The source queryable to apply parameters to.</param>
    /// <returns>A tuple containing the count query (before paging) and the paged query.</returns>
    public (IQueryable<T> countQuery, IQueryable<T> pagedQuery) ApplyToIQueryable<T>(IQueryable<T> query) {
        return ApplyToIQueryableInternal<T>(query, null);
    }

    /// <summary>
    /// Applies search parameters to the queryable with optional field-level restrictions.
    /// Restricted fields are silently ignored.
    /// </summary>
    /// <param name="query">The source queryable to apply parameters to.</param>
    /// <param name="configureRestrictions">Optional action to configure field restrictions using a fluent builder.</param>
    /// <returns>A tuple containing the count query (before paging) and the paged query.</returns>
    public (IQueryable<T> countQuery, IQueryable<T> pagedQuery) ApplyToIQueryable<T>(
        IQueryable<T> query,
        Action<FieldRestrictions>? configureRestrictions) {
        if (configureRestrictions == null) return ApplyToIQueryableInternal<T>(query, null);
        var restrictions = new FieldRestrictions();
        configureRestrictions(restrictions);
        return ApplyToIQueryableInternal(query, restrictions);
    }

    /// <summary>
    /// Applies search parameters to the queryable with optional field-level restrictions.
    /// Restricted fields are silently ignored.
    /// </summary>
    /// <param name="query">The source queryable to apply parameters to.</param>
    /// <param name="restrictions">Optional pre-configured field restrictions instance.</param>
    /// <returns>A tuple containing the count query (before paging) and the paged query.</returns>
    public (IQueryable<T> countQuery, IQueryable<T> pagedQuery) ApplyToIQueryable<T>(
        IQueryable<T> query,
        FieldRestrictions? restrictions) {
        return ApplyToIQueryableInternal(query, restrictions);
    }

    private (IQueryable<T> countQuery, IQueryable<T> pagedQuery) ApplyToIQueryableInternal<T>(
        IQueryable<T> query,
        FieldRestrictions? restrictions) {

        if (Filters.Count != 0) {
            query = Filters.Aggregate(query, (current, filter) => filter.ApplyFilter(current, restrictions));
        }

        if (Sortings.Count != 0) {
            query = ApplySortings(query, restrictions);
        }

        var countQuery = query;

        if (Paging != null) query = query.Paging(Paging);

        return (countQuery, query);
    }

    private IQueryable<T> ApplySortings<T>(IQueryable<T> query, FieldRestrictions? restrictions) {
        var distinctSortings = Sortings.DistinctBy(x => x.PropertyName).ToArray();
        var (orderedQuery, startIndex) = GetFirstAllowedSorting(query, distinctSortings, restrictions);

        if (orderedQuery == null) return query;

        return distinctSortings.Skip(startIndex)
            .Aggregate(orderedQuery, (current, sorting) => sorting.ThenApplyToIQueryable(current, restrictions));
    }

    private static (IOrderedQueryable<T>? query, int nextIndex) GetFirstAllowedSorting<T>(
        IQueryable<T> query,
        Sorting[] sortings,
        FieldRestrictions? restrictions) {

        for (var i = 0; i < sortings.Length; i++) {
            var orderedQuery = sortings[i].ApplyToIQueryable(query, restrictions);
            if (orderedQuery != null) {
                return (orderedQuery, i + 1);
            }
        }

        return (null, sortings.Length);
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
        var split = QueryStringHelpers.SplitQueryStringInList(dict);

        var paging = Paging.FromDictionary(dict);
        var sorting = split.TryGetValue("sortings", out var value) ? value.Select(Sorting.FromDictionary) : null;
        var filters = split.TryGetValue("filters", out var value1) ? value1.Select(Filter.FromDictionary) : null;
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