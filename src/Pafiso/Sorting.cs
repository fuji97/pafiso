using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Pafiso.Extensions;
using Pafiso.Util;

namespace Pafiso;

[method: JsonConstructor]
public class Sorting(string propertyName, SortOrder sortOrder) {
    public string PropertyName { get; } = propertyName;
    public SortOrder SortOrder { get;} = sortOrder;

    public bool Ascending => SortOrder == SortOrder.Ascending;
    public bool Descending => SortOrder == SortOrder.Descending;

    public static Sorting<T> FromExpression<T>(Expression<Func<T, object>> expr, SortOrder order) {
        var field = ExpressionUtilities.ExpressionDecomposer(expr.Body);
        return new Sorting<T>(field, order);
    }

    public IOrderedQueryable<T> ApplyToIQueryable<T>(IQueryable<T> query) {
        var expr = ExpressionUtilities.BuildLambdaExpression<T,object>(PropertyName);

        return Ascending ? query.OrderBy(expr) : query.OrderByDescending(expr);
    }
    
    public IOrderedQueryable<T> ThenApplyToIQueryable<T>(IOrderedQueryable<T> query) {
        var expr = ExpressionUtilities.BuildLambdaExpression<T,object>(PropertyName);

        return Ascending ? query.ThenBy(expr) : query.ThenByDescending(expr);
    }
    
    public IDictionary<string,string> ToDictionary() {
        return new Dictionary<string, string> {
            { "prop", PropertyName },
            { "ord", SortOrder.ToEnumMemberValue() }
        };
    }

    public static Sorting FromDictionary(IDictionary<string, string> dict) {
        return new Sorting(dict["prop"], EnumExtensions.ParseEnumMember<SortOrder>(dict["ord"]));
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

public class Sorting<T>(string propertyName, SortOrder sortOrder) : Sorting(propertyName, sortOrder);