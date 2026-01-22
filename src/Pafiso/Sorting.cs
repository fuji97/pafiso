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

    /// <summary>
    /// Applies sorting to the queryable with optional field-level restrictions.
    /// </summary>
    /// <param name="query">The source queryable to apply sorting to.</param>
    /// <param name="restrictions">Optional field restrictions instance.</param>
    /// <returns>The sorted queryable, or null if the sort field is not allowed.</returns>
    public IOrderedQueryable<T>? ApplyToIQueryable<T>(IQueryable<T> query, FieldRestrictions? restrictions) {
        if (restrictions == null) return ApplyToIQueryable(query);
        if (!restrictions.IsSortFieldAllowed(PropertyName)) return null;
        return ApplyToIQueryable(query);
    }

    public IOrderedQueryable<T> ThenApplyToIQueryable<T>(IOrderedQueryable<T> query) {
        var expr = ExpressionUtilities.BuildLambdaExpression<T,object>(PropertyName);

        return Ascending ? query.ThenBy(expr) : query.ThenByDescending(expr);
    }

    /// <summary>
    /// Applies secondary sorting to the queryable with optional field-level restrictions.
    /// </summary>
    /// <param name="query">The source ordered queryable to apply sorting to.</param>
    /// <param name="restrictions">Optional field restrictions instance.</param>
    /// <returns>The sorted queryable. If the sort field is not allowed, returns the original query unchanged.</returns>
    public IOrderedQueryable<T> ThenApplyToIQueryable<T>(IOrderedQueryable<T> query, FieldRestrictions? restrictions) {
        if (restrictions == null) return ThenApplyToIQueryable(query);
        if (!restrictions.IsSortFieldAllowed(PropertyName)) return query;
        return ThenApplyToIQueryable(query);
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