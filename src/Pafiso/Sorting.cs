using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Pafiso;

public class Sorting {
    public string PropertyName { get; }
    public SortOrder SortOrder { get;}
    
    public bool Ascending => SortOrder == SortOrder.Ascending;
    public bool Descending => SortOrder == SortOrder.Descending;
    
    [JsonConstructor]
    public Sorting(string propertyName, SortOrder sortOrder) {
        PropertyName = propertyName;
        SortOrder = sortOrder;
    }

    public static Sorting FromExpression<T>(Expression<Func<T, object>> expr, SortOrder order) {
        if (expr.Body is not MemberExpression member)
            throw new ArgumentException("Expression must be a member access expression", nameof(expr));
        return new Sorting(member.Member.Name, order);
    }

    public IOrderedQueryable<T> ApplyToIQueryable<T>(IQueryable<T> query) {
        var prop = TypeDescriptor.GetProperties(typeof(T)).Find(PropertyName, false);
        
        if (prop is null)
            throw new ArgumentException($"Property {PropertyName} not found", nameof(PropertyName));
        
        return Ascending ? query.OrderBy(x => prop.GetValue(x)) : query.OrderByDescending(x => prop.GetValue(x));
    }
    
    public IOrderedQueryable<T> ThenApplyToIQueryable<T>(IOrderedQueryable<T> query) {
        var prop = TypeDescriptor.GetProperties(typeof(T)).Find(PropertyName, false);
        
        if (prop is null)
            throw new ArgumentException($"Property {PropertyName} not found", nameof(PropertyName));
        
        return Ascending ? query.ThenBy(x => prop.GetValue(x)) : query.ThenByDescending(x => prop.GetValue(x));
    }
    
    public IDictionary<string,string> ToDictionary() {
        return new Dictionary<string, string> {
            { "prop", PropertyName },
            { "ord", SortOrder.ToString() }
        };
    }
    
    public static Sorting FromDictionary(IDictionary<string, string> dict) {
        return new Sorting(dict["prop"], Enum.Parse<SortOrder>(dict["ord"]));
    }

    public override string ToString() {
        return $"{PropertyName} ({SortOrder})";
    }

    public bool Equals(Sorting other) {
        return PropertyName == other.PropertyName && SortOrder == other.SortOrder;
    }

    public override bool Equals(object? obj) {
        return obj is Sorting other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(PropertyName, (int)SortOrder);
    }

    public static bool operator ==(Sorting left, Sorting right) {
        return left.Equals(right);
    }

    public static bool operator !=(Sorting left, Sorting right) {
        return !left.Equals(right);
    }
}