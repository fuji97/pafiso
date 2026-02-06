using System.Text.Json;
using Pafiso.Enumerables;
using Pafiso.Extensions;
using Pafiso.Mapping;
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
    /// Applies search parameters to the queryable with the specified settings.
    /// </summary>
    /// <param name="query">The source queryable to apply parameters to.</param>
    /// <param name="settings">The settings to use for field name resolution and string comparison.</param>
    /// <returns>A tuple containing the count query (before paging) and the paged query.</returns>
    public (IQueryable<T> countQuery, IQueryable<T> pagedQuery) ApplyToIQueryable<T>(
        IQueryable<T> query,
        PafisoSettings? settings) {
        return ApplyToIQueryableInternal(query, settings);
    }



    private (IQueryable<T> countQuery, IQueryable<T> pagedQuery) ApplyToIQueryableInternal<T>(
        IQueryable<T> query,
        PafisoSettings? settings) {

        if (Filters.Count != 0) {
            query = Filters.Aggregate(query, (current, filter) => filter.ApplyFilter(current, settings));
        }

        if (Sortings.Count != 0) {
            query = ApplySortings(query, settings);
        }

        var countQuery = query;

        if (Paging != null) query = query.Paging(Paging);

        return (countQuery, query);
    }

    private IQueryable<T> ApplySortings<T>(IQueryable<T> query, PafisoSettings? settings) {
        var distinctSortings = Sortings.DistinctBy(x => x.PropertyName).ToArray();
        var (orderedQuery, startIndex) = GetFirstAllowedSorting(query, distinctSortings, settings);

        if (orderedQuery == null) return query;

        return distinctSortings.Skip(startIndex)
            .Aggregate(orderedQuery, (current, sorting) => sorting.ThenApplyToIQueryable(current, settings));
    }

    private static (IOrderedQueryable<T>? query, int nextIndex) GetFirstAllowedSorting<T>(
        IQueryable<T> query,
        Sorting[] sortings,
        PafisoSettings? settings) {

        for (var i = 0; i < sortings.Length; i++) {
            var orderedQuery = sortings[i].ApplyToIQueryable(query, settings);
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


    /// <summary>
    /// Creates a SearchParameters instance from a dictionary representation using a field mapper.
    /// Field names in the dictionary will be resolved using the mapper during query execution.
    /// </summary>
    /// <typeparam name="TMapping">The mapping model type (DTO) that must inherit from <see cref="MappingModel"/>.</typeparam>
    /// <typeparam name="TEntity">The entity type (database model) to map to.</typeparam>
    /// <param name="dict">The dictionary representation of search parameters.</param>
    /// <param name="mapper">The field mapper instance for resolving field names.</param>
    /// <param name="jsonOptions">Optional JSON serializer options for deserialization.</param>
    /// <returns>A new SearchParameters instance with mapper-enabled filters and sortings.</returns>
    public static SearchParameters FromDictionary<TMapping, TEntity>(
        IDictionary<string, string> dict,
        IFieldMapper<TMapping, TEntity> mapper,
        JsonSerializerOptions? jsonOptions = null)
        where TMapping : MappingModel {

        var split = QueryStringHelpers.SplitQueryStringInList(dict);

        var paging = Paging.FromDictionary(dict);

        // Create sortings with mapper embedded
        var sortings = new List<Sorting>();
        if (split.TryGetValue("sortings", out var sortingDicts)) {
            foreach (var sortingDict in sortingDicts) {
                var propertyName = sortingDict["prop"];
                var sortOrder = EnumExtensions.ParseEnumMember<SortOrder>(sortingDict["ord"]);
                sortings.Add(new Sorting(propertyName, sortOrder, mapper));
            }
        }

        // Create filters with mapper embedded
        var filters = new List<Filter>();
        if (split.TryGetValue("filters", out var filterDicts)) {
            foreach (var filterDict in filterDicts) {
                var fields = filterDict["fields"]!.Split(",");
                var op = filterDict["op"]!;
                filterDict.TryGetValue("val", out var val);
                var caseSensitive = filterDict.ContainsKey("case") && filterDict["case"] == "true";
                filters.Add(new Filter(fields, EnumExtensions.ParseEnumMember<FilterOperator>(op), val, mapper, caseSensitive));
            }
        }

        return new SearchParameters {
            Paging = paging,
            Sortings = sortings,
            Filters = filters
        };
    }

    /// <summary>
    /// Creates a SearchParameters instance from JSON using a field mapper.
    /// </summary>
    /// <typeparam name="TMapping">The mapping model type (DTO) that must inherit from <see cref="MappingModel"/>.</typeparam>
    /// <typeparam name="TEntity">The entity type (database model) to map to.</typeparam>
    /// <param name="json">The JSON string containing search parameters.</param>
    /// <param name="mapper">The field mapper instance for resolving field names.</param>
    /// <param name="jsonOptions">Optional JSON serializer options for deserialization.</param>
    /// <returns>A new SearchParameters instance with mapper-enabled filters and sortings.</returns>
    public static SearchParameters FromJson<TMapping, TEntity>(
        string json,
        IFieldMapper<TMapping, TEntity> mapper,
        JsonSerializerOptions? jsonOptions = null)
        where TMapping : MappingModel {

        // Deserialize the JSON to a temporary SearchParameters
        var tempParams = JsonSerializer.Deserialize<SearchParameters>(json, jsonOptions);
        if (tempParams == null) {
            return new SearchParameters();
        }

        // Recreate filters with mapper embedded
        var filters = new List<Filter>();
        foreach (var filter in tempParams.Filters) {
            filters.Add(new Filter(filter.Fields, filter.Operator, filter.Value, mapper, filter.CaseSensitive));
        }

        // Recreate sortings with mapper embedded
        var sortings = new List<Sorting>();
        foreach (var sorting in tempParams.Sortings) {
            sortings.Add(new Sorting(sorting.PropertyName, sorting.SortOrder, mapper));
        }

        return new SearchParameters {
            Paging = tempParams.Paging,
            Sortings = sortings,
            Filters = filters
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
