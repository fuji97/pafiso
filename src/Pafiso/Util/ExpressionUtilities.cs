using System.Linq.Expressions;

namespace Pafiso.Util; 

public static class ExpressionUtilities {
    /// <summary>
    /// Delegate for building EF Core Like expressions. This is set by the Pafiso.EntityFrameworkCore package.
    /// </summary>
    public static Func<Expression, string, Expression>? EfCoreLikeExpressionBuilder { get; set; }

    public static string ExpressionDecomposer(Expression expr) {
        while (true) {
            switch (expr) {
                case MemberExpression member: {
                    var field = member.Member.Name;

                    if (member.Expression is MemberExpression memberExpression) {
                        return $"{ExpressionDecomposer(memberExpression)}.{field}";
                    }

                    return field;
                }
                case UnaryExpression unary:
                    expr = unary.Operand;
                    continue;
                default:
                    throw new ArgumentException("Expression must be a member or unary expression");
            }
        }
    }

    public static object GetValue(MemberExpression member) {
        var objectMember = Expression.Convert(member, typeof(object));
        var getterLambda = Expression.Lambda<Func<object>>(objectMember);
        var getter = getterLambda.Compile();
        return getter();
    }
    
    public static object GetValue<T>(MemberExpression member) {
        var objectMember = Expression.Convert(member, typeof(T));
        var getterLambda = Expression.Lambda<Func<T>>(objectMember);
        var getter = getterLambda.Compile();
        return getter() ?? throw new InvalidOperationException();
    }

    public static string? GetExpressionValue(Expression expr) {
        var value = expr switch {
            ConstantExpression constantExpression => constantExpression.Value?.ToString(),
            MemberExpression rightMember => GetValue(rightMember).ToString(),
            _ => throw new InvalidOperationException("Invalid expression")
        };

        return value;
    }

    public static (string path, FilterOperator op, string? value) DecomposeMethodCallExpression(MethodCallExpression expr) {
        if (expr.Object == null) {
            throw new InvalidOperationException("The method must be called on an object. Static method calls are not supported.");
        }
        var path = ExpressionDecomposer(expr.Object);
        switch (expr.Method.Name) {
                case "Contains":
                    var value = GetMethodArgumentValues(expr).FirstOrDefault();
                    return (path, FilterOperator.Contains, value);
        }
        throw new InvalidOperationException("Unsupported expression");
    }

    public static (string path, FilterOperator op, string? value) DecomposeUnaryWrapperExpression(UnaryExpression expr) {
        var methodExpression = expr.Operand as MethodCallExpression;
        if (methodExpression == null) {
            throw new InvalidOperationException("Unsupported expression");
        }
        var (path, op, value) = DecomposeMethodCallExpression(methodExpression);
        switch (expr.NodeType) {
            case ExpressionType.Not:
                switch (op) {
                    case FilterOperator.Contains:
                        return (path, FilterOperator.NotContains, value);
                }
                break;
        }
        throw new InvalidOperationException("Unsupported expression");
    }
    
    public static IEnumerable<string> GetMethodArgumentValues(MethodCallExpression expr) {
        return expr.Arguments.Select(GetExpressionValue).Where(x => x != null).Cast<string>();
    }

    public static FilterOperator ToFilterOperator(this ExpressionType type, string? value) {
        var operatorName = type switch {
            ExpressionType.Equal => FilterOperator.Equals,
            ExpressionType.NotEqual => FilterOperator.NotEquals,
            ExpressionType.GreaterThan => FilterOperator.GreaterThan,
            ExpressionType.GreaterThanOrEqual => FilterOperator.GreaterThanOrEquals,
            ExpressionType.LessThan => FilterOperator.LessThan,
            ExpressionType.LessThanOrEqual => FilterOperator.LessThanOrEquals,
            _ => throw new InvalidOperationException("Expression must be a binary expression")
        };
        
        // Convert to null check if value is null and operator is equals or not equals
        if (value == null) {
            operatorName = operatorName switch {
                FilterOperator.Equals => FilterOperator.Null,
                FilterOperator.NotEquals => FilterOperator.NotNull,
                _ => operatorName
            };
        }

        return operatorName;
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
            var temp = propName.Split(['.'], 2);
            return GetPropertyValue(GetPropertyValue(src, temp[0]), temp[1]);
        }
        else {
            var prop = src.GetType().GetProperty(propName);
            return prop != null ? prop.GetValue(src, null) : null;
        }
    }
    
    public static string GetStringPropertyValue<T>(T obj, string propName, bool caseSensitive) {
        var propValue = GetPropertyValue(obj, propName);
        if (propValue == null) {
            throw new ArgumentNullException($"Null value: {propName}");
        }
        
        // Cast to long if enum
        if (propValue is Enum) {
            propValue = Convert.ToInt64(propValue);
        }
        
        var value = propValue.ToString();
        if (!caseSensitive) {
            value = value?.ToLower();
        }
        if (value == null) {
            throw new ArgumentNullException($"Null value: {propName}");
        }

        return value;
    }

    /// <summary>
    /// From: https://stackoverflow.com/questions/16208214/construct-lambdaexpression-for-nested-property-from-string
    /// </summary>
    /// <param name="propName"></param>
    /// <param name="paramName"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static Expression<Func<T,TResult>> BuildLambdaExpression<T,TResult> (string propName, string paramName = "x") {
        var (param, body) = ParameterExpression<T, TResult>(propName, paramName);
        if (body.Type != typeof(TResult)) {
            body = Expression.Convert(body, typeof(TResult));
        }
        return Expression.Lambda<Func<T,TResult>>(body, param);
    }
    private static (ParameterExpression param, Expression body) ParameterExpression<T, TResult>(string propName, string paramName) {
        var param = Expression.Parameter(typeof(T), paramName);
        Expression body = param;
        foreach (var member in propName.Split('.')) {
            body = Expression.PropertyOrField(body, member);
        }
        return (param, body);
    }
    
    private static Expression BuildContainsExpression<T>(Expression memberExpression, string? value, bool contains, bool caseSensitive) {
        if (value == null) {
            return Expression.Constant(false);
        }
        
        var valueParam = Expression.Constant(value);
        
        if (memberExpression.Type != typeof(string)) {
            memberExpression = Expression.Convert(memberExpression, typeof(string));
        }
        
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        if (!caseSensitive) {
            var lowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
            memberExpression = Expression.Call(memberExpression, lowerMethod);
        }
        var methodCallExpression = Expression.Call(memberExpression, containsMethod, valueParam);
        if (contains) {
            return methodCallExpression;
        }
        else {
            return Expression.Not(methodCallExpression);
        }
    }

    private static Expression BuildContainsExpression<T>(
        Expression memberExpression,
        string? value,
        bool contains,
        bool caseSensitive,
        PafisoSettings settings) {
        if (value == null) {
            return Expression.Constant(false);
        }

        if (memberExpression.Type != typeof(string)) {
            memberExpression = Expression.Convert(memberExpression, typeof(string));
        }

        // Try to use EF Core Like if available and configured
        if (!caseSensitive && settings.UseEfCoreLikeForCaseInsensitive && EfCoreLikeExpressionBuilder != null) {
            var pattern = $"%{EscapeLikePattern(value)}%";
            var likeExpression = EfCoreLikeExpressionBuilder(memberExpression, pattern);
            return contains ? likeExpression : Expression.Not(likeExpression);
        }

        // Use StringComparison for case-insensitive matching
        if (!caseSensitive) {
            // Use string.Contains(string, StringComparison) overload
            var containsWithComparisonMethod = typeof(string).GetMethod(
                nameof(string.Contains),
                [typeof(string), typeof(StringComparison)])!;
            var valueParam = Expression.Constant(value);
            var comparisonParam = Expression.Constant(settings.StringComparison);
            var methodCallExpression = Expression.Call(memberExpression, containsWithComparisonMethod, valueParam, comparisonParam);
            return contains ? methodCallExpression : Expression.Not(methodCallExpression);
        }

        // Case-sensitive: use simple Contains
        var simpleContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var simpleValueParam = Expression.Constant(value);
        var simpleMethodCall = Expression.Call(memberExpression, simpleContainsMethod, simpleValueParam);
        return contains ? simpleMethodCall : Expression.Not(simpleMethodCall);
    }

    private static string EscapeLikePattern(string value) {
        // Escape special LIKE pattern characters
        return value
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }
    
    private static Expression BuildComparisonExpression<TValue>(Expression memberExpression, FilterOperator op, TValue value, bool caseSensitive) {
        switch (op) {
            case FilterOperator.Contains:
                return BuildContainsExpression<TValue>(memberExpression, value?.ToString(), true, caseSensitive);
            case FilterOperator.NotContains:
                return BuildContainsExpression<TValue>(memberExpression, value?.ToString(), false, caseSensitive);
            case FilterOperator.Null:
                return Expression.ReferenceEqual(memberExpression, Expression.Constant(null));
            case FilterOperator.NotNull:
                return Expression.Not(Expression.ReferenceEqual(memberExpression, Expression.Constant(null)));
        }

        var valueExpression = Expression.Constant(value);
        if (memberExpression.Type != typeof(TValue)) {
            memberExpression = Expression.Convert(memberExpression, typeof(TValue));
        }
        if (typeof(TValue) == typeof(string) && !caseSensitive) {
            memberExpression = Expression.Call(memberExpression, nameof(string.ToLower), Type.EmptyTypes);
        }
        

        switch (op) {
            case FilterOperator.Equals:
                return Expression.Equal(memberExpression, valueExpression);
            case FilterOperator.NotEquals:
                return Expression.NotEqual(memberExpression, valueExpression);
            case FilterOperator.GreaterThan:
                return Expression.GreaterThan(memberExpression, valueExpression);
            case FilterOperator.LessThan:
                return Expression.LessThan(memberExpression, valueExpression);
            case FilterOperator.GreaterThanOrEquals:
                return Expression.GreaterThanOrEqual(memberExpression, valueExpression);
            case FilterOperator.LessThanOrEquals:
                return Expression.LessThanOrEqual(memberExpression, valueExpression);
        }
        
        throw new ArgumentOutOfRangeException(nameof(op), op, null);
    }

    private static Expression BuildComparisonExpression<TValue>(
        Expression memberExpression,
        FilterOperator op,
        TValue value,
        bool caseSensitive,
        PafisoSettings settings) {
        switch (op) {
            case FilterOperator.Contains:
                return BuildContainsExpression<TValue>(memberExpression, value?.ToString(), true, caseSensitive, settings);
            case FilterOperator.NotContains:
                return BuildContainsExpression<TValue>(memberExpression, value?.ToString(), false, caseSensitive, settings);
            case FilterOperator.Null:
                return Expression.ReferenceEqual(memberExpression, Expression.Constant(null));
            case FilterOperator.NotNull:
                return Expression.Not(Expression.ReferenceEqual(memberExpression, Expression.Constant(null)));
        }

        if (memberExpression.Type != typeof(TValue)) {
            memberExpression = Expression.Convert(memberExpression, typeof(TValue));
        }

        // Handle string comparisons with StringComparison
        if (typeof(TValue) == typeof(string) && !caseSensitive) {
            return BuildStringComparisonExpression(memberExpression, op, value?.ToString(), settings);
        }

        var valueExpression = Expression.Constant(value);

        switch (op) {
            case FilterOperator.Equals:
                return Expression.Equal(memberExpression, valueExpression);
            case FilterOperator.NotEquals:
                return Expression.NotEqual(memberExpression, valueExpression);
            case FilterOperator.GreaterThan:
                return Expression.GreaterThan(memberExpression, valueExpression);
            case FilterOperator.LessThan:
                return Expression.LessThan(memberExpression, valueExpression);
            case FilterOperator.GreaterThanOrEquals:
                return Expression.GreaterThanOrEqual(memberExpression, valueExpression);
            case FilterOperator.LessThanOrEquals:
                return Expression.LessThanOrEqual(memberExpression, valueExpression);
        }

        throw new ArgumentOutOfRangeException(nameof(op), op, null);
    }

    private static Expression BuildStringComparisonExpression(
        Expression memberExpression,
        FilterOperator op,
        string? value,
        PafisoSettings settings) {
        
        // Try to use EF Core Like for Equals operations if available
        if (settings.UseEfCoreLikeForCaseInsensitive && EfCoreLikeExpressionBuilder != null) {
            switch (op) {
                case FilterOperator.Equals: {
                    var pattern = EscapeLikePattern(value ?? "");
                    return EfCoreLikeExpressionBuilder(memberExpression, pattern);
                }
                case FilterOperator.NotEquals: {
                    var pattern = EscapeLikePattern(value ?? "");
                    return Expression.Not(EfCoreLikeExpressionBuilder(memberExpression, pattern));
                }
            }
        }

        // Use string.Equals with StringComparison for Equals/NotEquals
        if (op == FilterOperator.Equals || op == FilterOperator.NotEquals) {
            var equalsMethod = typeof(string).GetMethod(
                nameof(string.Equals),
                [typeof(string), typeof(string), typeof(StringComparison)])!;
            var valueParam = Expression.Constant(value);
            var comparisonParam = Expression.Constant(settings.StringComparison);
            var equalsCall = Expression.Call(null, equalsMethod, memberExpression, valueParam, comparisonParam);
            return op == FilterOperator.Equals ? equalsCall : Expression.Not(equalsCall);
        }

        // For comparison operators (>, <, >=, <=), use string.Compare with StringComparison
        var compareMethod = typeof(string).GetMethod(
            nameof(string.Compare),
            [typeof(string), typeof(string), typeof(StringComparison)])!;
        var valueExpr = Expression.Constant(value);
        var comparisonExpr = Expression.Constant(settings.StringComparison);
        var compareCall = Expression.Call(null, compareMethod, memberExpression, valueExpr, comparisonExpr);
        var zero = Expression.Constant(0);

        return op switch {
            FilterOperator.GreaterThan => Expression.GreaterThan(compareCall, zero),
            FilterOperator.LessThan => Expression.LessThan(compareCall, zero),
            FilterOperator.GreaterThanOrEquals => Expression.GreaterThanOrEqual(compareCall, zero),
            FilterOperator.LessThanOrEquals => Expression.LessThanOrEqual(compareCall, zero),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    /// <summary>
    /// Builds a filter expression using legacy ToLower() approach for backward compatibility.
    /// </summary>
    public static Expression<Func<T,bool>> BuildFilterExpression<T>(string propName, string paramName, FilterOperator op, string? value, bool caseSensitive) {
        var (param, body) = ParameterExpression<T, bool>(propName, paramName);

        if (!caseSensitive) {
            value = value?.ToLower();
        }
        Expression comparison;
        if (value == null) {
            comparison = BuildComparisonExpression(body, op, value, false);
        } 
        else if (float.TryParse(value, out var floatValue)) {
            comparison = BuildComparisonExpression(body, op, floatValue, false);
        }
        else if (bool.TryParse(value, out var boolValue)) {
            comparison = BuildComparisonExpression(body, op, boolValue, false);
        }
        else if (long.TryParse(value, out var longValue)) {
            comparison = BuildComparisonExpression(body, op, longValue, false);
        }
        else {
            comparison = BuildComparisonExpression(body, op, value, caseSensitive);
        }
        
        return Expression.Lambda<Func<T,bool>>(comparison, param);
    }

    /// <summary>
    /// Builds a filter expression using the specified settings for string comparison.
    /// </summary>
    public static Expression<Func<T, bool>> BuildFilterExpression<T>(
        string propName,
        string paramName,
        FilterOperator op,
        string? value,
        bool caseSensitive,
        PafisoSettings settings) {
        var (param, body) = ParameterExpression<T, bool>(propName, paramName);

        Expression comparison;
        if (value == null) {
            comparison = BuildComparisonExpression(body, op, value, false, settings);
        }
        else if (float.TryParse(value, out var floatValue)) {
            comparison = BuildComparisonExpression(body, op, floatValue, false, settings);
        }
        else if (bool.TryParse(value, out var boolValue)) {
            comparison = BuildComparisonExpression(body, op, boolValue, false, settings);
        }
        else if (long.TryParse(value, out var longValue)) {
            comparison = BuildComparisonExpression(body, op, longValue, false, settings);
        }
        else {
            comparison = BuildComparisonExpression(body, op, value, caseSensitive, settings);
        }

        return Expression.Lambda<Func<T, bool>>(comparison, param);
    }
}
