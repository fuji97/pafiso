using Pafiso.Enumerables;
using Pafiso.Util;

namespace Pafiso; 

[Serializable]
public class SearchParameters {

    public Paging? Paging { get; set; } = null;
    public List<Sorting> Sortings { get; set; } = new();
    public List<Filter> Filters { get; set; } = new();

    public SearchParameters AddSorting(params Sorting[] sorting) {
        Sortings.AddRange(sorting);
        return this;
    }
    
    public SearchParameters AddFilters(params Filter[] filters) {
        Filters.AddRange(filters);
        return this;
    }

    public PagedQueryable<T> ApplyToIQueryable<T>(IQueryable<T> query) {
        if (Filters.Any()) {
            foreach (var filter in Filters) {
                query = query.Where(filter);
            }
        }
        
        if (Sortings.Any()) {
            var orderedQuery = query.OrderBy(Sortings.First());
            query = Sortings.Skip(1).Aggregate(orderedQuery, 
                (current, sorting) => current.ThenBy(sorting));
            
        }

        var count = query.Count();

        if (Paging != null) {
            query = query.Paging(Paging);
        }
        
        return new PagedQueryable<T>(count, query);
    }

    public IDictionary<string, string> ToDictionary() {
        var dicts = new List<IDictionary<string, string>>() {
            QueryStringHelpers.MergeListOfQueryStrings("sortings",
                Sortings.Select(s => s.ToDictionary())),
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
            Sortings = sorting?.ToList() ?? new List<Sorting>(),
            Filters = filters?.ToList() ?? new List<Filter>()
        };
    }

    protected bool Equals(SearchParameters other) {
        return Nullable.Equals(Paging, other.Paging) && Sortings.SequenceEqual(other.Sortings) && Filters.SequenceEqual(other.Filters);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
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
}