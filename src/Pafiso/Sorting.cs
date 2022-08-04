using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Pafiso.Util;

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
        var field = ExpressionUtilities.ExpressionDecomposer(expr.Body);
        return new Sorting(field, order);
    }

    public IOrderedQueryable<T> ApplyToIQueryable<T>(IQueryable<T> query) {
        var expr = ExpressionUtilities.BuildExpression<T,object>(PropertyName);

        return Ascending ? query.OrderBy(expr) : query.OrderByDescending(expr);
    }
    
    public IOrderedQueryable<T> ThenApplyToIQueryable<T>(IOrderedQueryable<T> query) {
        var expr = ExpressionUtilities.BuildExpression<T,object>(PropertyName);

        return Ascending ? query.ThenBy(expr) : query.ThenByDescending(expr);
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

public class Sorting<T> : Sorting {
    public Sorting(string propertyName, SortOrder sortOrder) : base(propertyName, sortOrder) {
    }
    
    public static Sorting FromExpression(Expression<Func<T, object>> expr, SortOrder order) {
        MemberExpression? memberExpression;
        if (expr.Body is MemberExpression member) {
            memberExpression = member;
        } else if (expr.Body is UnaryExpression unary) {
            memberExpression = unary.Operand as MemberExpression;
        } else {
            throw new InvalidOperationException("Expression must be a member expression");
        }

        if (memberExpression == null) {
            throw new InvalidOperationException("Expression must be a member expression");
        }
        
        return new Sorting(memberExpression.Member.Name, order);
    }
}