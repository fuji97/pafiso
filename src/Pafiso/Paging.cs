using System.Text.Json.Serialization;

namespace Pafiso; 

public class Paging {
    private const int StartingPage = 1;
    
    public int Take { get; private init; }
    public int Skip { get; private init; }

    [JsonIgnore]
    public int Page => (Skip / Take) + StartingPage;
    [JsonIgnore]
    public int PageSize => Take;

    public Paging() {
    }

    [JsonConstructor]
    public Paging(int take, int skip) {
        Take = take;
        Skip = skip;
    }

    public static Paging FromPaging(int page, int pageSize) {
        if (page < 0 || pageSize < 1) {
            throw new ArgumentException("Page size must be greater than 0 and page can't be less than 0");
        }
        
        return new Paging {
            Skip = (page - StartingPage) * pageSize,
            Take = pageSize
        };
    }
    
    public static Paging FromSkipTake(int skip, int take) {
        if (skip < 0 || take < 0) {
            throw new ArgumentException("Skip and take must be greater than 0");
        }
        
        return new Paging {
            Skip = skip,
            Take = take
        };
    }
    
    public IQueryable<T> ApplyToIQueryable<T>(IQueryable<T> query) {
        return query.Skip(Skip).Take(Take);
    }
    
    public IDictionary<string,string> ToDictionary() {
        return new Dictionary<string, string> {
            ["skip"] = Skip.ToString(),
            ["take"] = Take.ToString()
        };
    }
    public static Paging? FromDictionary(IDictionary<string,string> dictionary) {
        if (!dictionary.ContainsKey("skip") || !dictionary.ContainsKey("take")) {
            return null;
        }
        
        return Paging.FromSkipTake(
            int.Parse(dictionary["skip"]),
            int.Parse(dictionary["take"])
        );
    }

    public bool Equals(Paging? other) {
        return Take == other.Take && Skip == other.Skip;
    }

    public override bool Equals(object? obj) {
        return obj is Paging other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Take, Skip);
    }

    public static bool operator ==(Paging left, Paging? right) {
        return left.Equals(right);
    }

    public static bool operator !=(Paging? left, Paging? right) {
        return !left.Equals(right);
    }

    public static Paging operator +(Paging a, int pages) {
        return new Paging() {
            Skip = a.Skip + a.Take * pages,
            Take = a.Take
        };
    }
    
    public static Paging operator -(Paging a, int pages) {
        return new Paging() {
            Skip = a.Skip - a.Take * pages,
            Take = a.Take
        };
    }

    public override string ToString() {
        return $"Page {Page} - Page size: {PageSize}";
    }
}