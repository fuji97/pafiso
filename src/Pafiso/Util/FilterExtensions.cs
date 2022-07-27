using System.Linq.Expressions;
using LinqKit;

namespace Pafiso.Util;

public static class FilterExtensions {
    public static Filter Or<T>(this Filter filter, Expression<Func<T, object>> fieldExpression) {
        var member = fieldExpression.Body as MemberExpression;
        if (member == null) {
            throw new InvalidOperationException("Expression must be a member expression");
        }
        var field = ExpressionUtilities.MemberDecomposer(member);
        filter.Fields.Add(field);
        return filter;
    }
    
    public static IQueryable<T> ApplyFilter<T>(Filter filter, IQueryable<T> query) {
        var value = filter.CaseSensitive ? filter.Value : filter.Value.ToLower();

        var predicatesBuilder = PredicateBuilder.New<T>();

        foreach (var field in filter.Fields) {
            predicatesBuilder.Or(ApplyCorrectOperation<T>(filter, field, value));
        }

        return query.Where(predicatesBuilder);
    }

    private static Expression<Func<T,bool>> ApplyCorrectOperation<T>(Filter filter, string field, string value) {
        switch (filter.Operator) {
            case FilterOperator.Equals:
                return (x => GetStringPropertyValue(x, field, filter.CaseSensitive) == value);
            case FilterOperator.NotEquals:
                return (x => GetStringPropertyValue(x, field, filter.CaseSensitive) != value);
            case FilterOperator.GreaterThan:
                return (x => float.Parse(GetStringPropertyValue(x, field, true)) > float.Parse(value));
            case FilterOperator.LessThan:
                return (x => float.Parse(GetStringPropertyValue(x, field, true)) <= float.Parse(value));
            case FilterOperator.GreaterThanOrEquals:
                return (x => float.Parse(GetStringPropertyValue(x, field, true)) > float.Parse(value));
            case FilterOperator.LessThanOrEquals:
                return (x => float.Parse(GetStringPropertyValue(x, field, true)) <= float.Parse(value));
            case FilterOperator.Contains:
                return (x => GetStringPropertyValue(x, field, filter.CaseSensitive).Contains(value));
            case FilterOperator.NotContains:
                return (x => !GetStringPropertyValue(x, field, filter.CaseSensitive).Contains(value));
            case FilterOperator.Null:
                return (x => GetPropertyValue(x, field) == null);
            case FilterOperator.NotNull:
                return (x => GetPropertyValue(x, field) != null);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static string GetStringPropertyValue<T>(T obj, string propName, bool caseSensitive) {
        var value = GetPropertyValue(obj, propName)?.ToString();
        if (!caseSensitive) {
            value = value?.ToLower();
        }
        if (value == null) {
            throw new ArgumentNullException($"Null value: {propName}");
        }

        return value;
    }

    /// <summary>
    /// Obtain value from nester property values.
    /// https://stackoverflow.com/questions/1954746/using-reflection-in-c-sharp-to-get-properties-of-a-nested-object
    /// </summary>
    /// <param name="src"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static object? GetPropertyValue(object? src, string propName)
    {
        //if (src == null) throw new ArgumentException("Value cannot be null.", "src");
        if (src == null) return null;
        if (propName == null) throw new ArgumentException("Value cannot be null.", nameof(propName));

        if(propName.Contains('.')) //complex type nested
        {
            var temp = propName.Split(new char[] { '.' }, 2);
            return GetPropertyValue(GetPropertyValue(src, temp[0]), temp[1]);
        }
        else {
            var prop = src.GetType().GetProperty(propName);
            return prop != null ? prop.GetValue(src, null) : null;
        }
    }

    public static IQueryable<T> Where<T>(this IQueryable<T> query, Filter filter) {
        return ApplyFilter(filter, query);
    }
    
    public static IEnumerable<T> Where<T>(this IEnumerable<T> query, Filter filter) {
        return ApplyFilter(filter, query.AsQueryable());
    }
}