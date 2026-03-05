using System.Linq.Expressions;
using Pafiso.Enums;

namespace Pafiso.Util; 

public static class ExpressionUtilities {

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
                    var containsValue = GetMethodArgumentValues(expr).FirstOrDefault();
                    return (path, FilterOperator.Contains, containsValue);
                case "StartsWith":
                    var startsWithValue = GetMethodArgumentValues(expr).FirstOrDefault();
                    return (path, FilterOperator.StartsWith, startsWithValue);
                case "EndsWith":
                    var endsWithValue = GetMethodArgumentValues(expr).FirstOrDefault();
                    return (path, FilterOperator.EndsWith, endsWithValue);
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
    
    /// <summary>
    /// Splits a comma-separated value string, supporting backslash escaping.
    /// <c>\,</c> produces a literal comma; <c>\\</c> produces a literal backslash.
    /// </summary>
    public static List<string> SplitEscapedValues(string value) {
        var results = new List<string>();
        var current = new System.Text.StringBuilder();
        for (var i = 0; i < value.Length; i++) {
            if (value[i] == '\\' && i + 1 < value.Length) {
                var next = value[i + 1];
                if (next == ',') {
                    current.Append(',');
                    i++;
                }
                else if (next == '\\') {
                    current.Append('\\');
                    i++;
                }
                else {
                    current.Append(value[i]);
                }
            }
            else if (value[i] == ',') {
                results.Add(current.ToString());
                current.Clear();
            }
            else {
                current.Append(value[i]);
            }
        }
        results.Add(current.ToString());
        return results;
    }

    private static Expression BuildInExpression(Expression memberExpression, string? value, bool negate, bool caseSensitive) {
        if (value == null) {
            return Expression.Constant(false);
        }

        var items = SplitEscapedValues(value);

        // Try to detect the type from the first item
        if (items.Count > 0 && float.TryParse(items[0], out _)) {
            var parsed = items.Select(float.Parse).ToList();
            var listExpr = Expression.Constant(parsed);
            if (memberExpression.Type != typeof(float)) {
                memberExpression = Expression.Convert(memberExpression, typeof(float));
            }
            var containsMethod = typeof(List<float>).GetMethod(nameof(List<float>.Contains), [typeof(float)])!;
            var call = Expression.Call(listExpr, containsMethod, memberExpression);
            return negate ? Expression.Not(call) : (Expression)call;
        }

        if (items.Count > 0 && bool.TryParse(items[0], out _)) {
            var parsed = items.Select(bool.Parse).ToList();
            var listExpr = Expression.Constant(parsed);
            if (memberExpression.Type != typeof(bool)) {
                memberExpression = Expression.Convert(memberExpression, typeof(bool));
            }
            var containsMethod = typeof(List<bool>).GetMethod(nameof(List<bool>.Contains), [typeof(bool)])!;
            var call = Expression.Call(listExpr, containsMethod, memberExpression);
            return negate ? Expression.Not(call) : (Expression)call;
        }

        if (items.Count > 0 && long.TryParse(items[0], out _)) {
            var parsed = items.Select(long.Parse).ToList();
            var listExpr = Expression.Constant(parsed);
            if (memberExpression.Type != typeof(long)) {
                memberExpression = Expression.Convert(memberExpression, typeof(long));
            }
            var containsMethod = typeof(List<long>).GetMethod(nameof(List<long>.Contains), [typeof(long)])!;
            var call = Expression.Call(listExpr, containsMethod, memberExpression);
            return negate ? Expression.Not(call) : (Expression)call;
        }

        // Default: string
        {
            var parsed = caseSensitive ? items : items.Select(s => s.ToLower()).ToList();
            var listExpr = Expression.Constant(parsed);
            if (memberExpression.Type != typeof(string)) {
                memberExpression = Expression.Convert(memberExpression, typeof(string));
            }
            if (!caseSensitive) {
                var lowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
                memberExpression = Expression.Call(memberExpression, lowerMethod);
            }
            var containsMethod = typeof(List<string>).GetMethod(nameof(List<string>.Contains), [typeof(string)])!;
            var call = Expression.Call(listExpr, containsMethod, memberExpression);
            return negate ? Expression.Not(call) : (Expression)call;
        }
    }

    private static Expression BuildInExpressionWithSettings(Expression memberExpression, string? value, bool negate, bool caseSensitive, PafisoSettings settings) {
        if (value == null) {
            return Expression.Constant(false);
        }

        var items = SplitEscapedValues(value);

        // Try to detect the type from the first item
        if (items.Count > 0 && float.TryParse(items[0], out _)) {
            var parsed = items.Select(float.Parse).ToList();
            var listExpr = Expression.Constant(parsed);
            if (memberExpression.Type != typeof(float)) {
                memberExpression = Expression.Convert(memberExpression, typeof(float));
            }
            var containsMethod = typeof(List<float>).GetMethod(nameof(List<float>.Contains), [typeof(float)])!;
            var call = Expression.Call(listExpr, containsMethod, memberExpression);
            return negate ? Expression.Not(call) : (Expression)call;
        }

        if (items.Count > 0 && bool.TryParse(items[0], out _)) {
            var parsed = items.Select(bool.Parse).ToList();
            var listExpr = Expression.Constant(parsed);
            if (memberExpression.Type != typeof(bool)) {
                memberExpression = Expression.Convert(memberExpression, typeof(bool));
            }
            var containsMethod = typeof(List<bool>).GetMethod(nameof(List<bool>.Contains), [typeof(bool)])!;
            var call = Expression.Call(listExpr, containsMethod, memberExpression);
            return negate ? Expression.Not(call) : (Expression)call;
        }

        if (items.Count > 0 && long.TryParse(items[0], out _)) {
            var parsed = items.Select(long.Parse).ToList();
            var listExpr = Expression.Constant(parsed);
            if (memberExpression.Type != typeof(long)) {
                memberExpression = Expression.Convert(memberExpression, typeof(long));
            }
            var containsMethod = typeof(List<long>).GetMethod(nameof(List<long>.Contains), [typeof(long)])!;
            var call = Expression.Call(listExpr, containsMethod, memberExpression);
            return negate ? Expression.Not(call) : (Expression)call;
        }

        // Default: string — use StringComparison-aware approach
        // For simplicity, we lowercase values and member when case-insensitive (same as legacy)
        {
            var parsed = caseSensitive ? items : items.Select(s => s.ToLower()).ToList();
            var listExpr = Expression.Constant(parsed);
            if (memberExpression.Type != typeof(string)) {
                memberExpression = Expression.Convert(memberExpression, typeof(string));
            }
            if (!caseSensitive) {
                var lowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
                memberExpression = Expression.Call(memberExpression, lowerMethod);
            }
            var containsMethod = typeof(List<string>).GetMethod(nameof(List<string>.Contains), [typeof(string)])!;
            var call = Expression.Call(listExpr, containsMethod, memberExpression);
            return negate ? Expression.Not(call) : (Expression)call;
        }
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

    private static Expression BuildStartsWithExpression<T>(Expression memberExpression, string? value, bool caseSensitive) {
        if (value == null) {
            return Expression.Constant(false);
        }

        var valueParam = Expression.Constant(value);

        if (memberExpression.Type != typeof(string)) {
            memberExpression = Expression.Convert(memberExpression, typeof(string));
        }

        var startsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
        if (!caseSensitive) {
            var lowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
            memberExpression = Expression.Call(memberExpression, lowerMethod);
        }
        return Expression.Call(memberExpression, startsWithMethod, valueParam);
    }

    private static Expression BuildEndsWithExpression<T>(Expression memberExpression, string? value, bool caseSensitive) {
        if (value == null) {
            return Expression.Constant(false);
        }

        var valueParam = Expression.Constant(value);

        if (memberExpression.Type != typeof(string)) {
            memberExpression = Expression.Convert(memberExpression, typeof(string));
        }

        var endsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
        if (!caseSensitive) {
            var lowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
            memberExpression = Expression.Call(memberExpression, lowerMethod);
        }
        return Expression.Call(memberExpression, endsWithMethod, valueParam);
    }
    
    private static Expression BuildContainsExpressionWithSettings(
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

        if (!caseSensitive) {
            var containsWithComparisonMethod = typeof(string).GetMethod(
                nameof(string.Contains),
                [typeof(string), typeof(StringComparison)])!;
            var valueParam = Expression.Constant(value);
            var comparisonParam = Expression.Constant(settings.StringComparison);
            var methodCallExpression = Expression.Call(memberExpression, containsWithComparisonMethod, valueParam, comparisonParam);
            return contains ? methodCallExpression : Expression.Not(methodCallExpression);
        }

        var simpleContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var simpleValueParam = Expression.Constant(value);
        var simpleMethodCall = Expression.Call(memberExpression, simpleContainsMethod, simpleValueParam);
        return contains ? simpleMethodCall : Expression.Not(simpleMethodCall);
    }

    private static Expression BuildStartsWithExpressionWithSettings(
        Expression memberExpression,
        string? value,
        bool caseSensitive,
        PafisoSettings settings) {
        if (value == null) {
            return Expression.Constant(false);
        }

        if (memberExpression.Type != typeof(string)) {
            memberExpression = Expression.Convert(memberExpression, typeof(string));
        }

        if (!caseSensitive) {
            var startsWithWithComparisonMethod = typeof(string).GetMethod(
                nameof(string.StartsWith),
                [typeof(string), typeof(StringComparison)])!;
            var valueParam = Expression.Constant(value);
            var comparisonParam = Expression.Constant(settings.StringComparison);
            return Expression.Call(memberExpression, startsWithWithComparisonMethod, valueParam, comparisonParam);
        }

        var simpleStartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
        var simpleValueParam = Expression.Constant(value);
        return Expression.Call(memberExpression, simpleStartsWithMethod, simpleValueParam);
    }

    private static Expression BuildEndsWithExpressionWithSettings(
        Expression memberExpression,
        string? value,
        bool caseSensitive,
        PafisoSettings settings) {
        if (value == null) {
            return Expression.Constant(false);
        }

        if (memberExpression.Type != typeof(string)) {
            memberExpression = Expression.Convert(memberExpression, typeof(string));
        }

        if (!caseSensitive) {
            var endsWithWithComparisonMethod = typeof(string).GetMethod(
                nameof(string.EndsWith),
                [typeof(string), typeof(StringComparison)])!;
            var valueParam = Expression.Constant(value);
            var comparisonParam = Expression.Constant(settings.StringComparison);
            return Expression.Call(memberExpression, endsWithWithComparisonMethod, valueParam, comparisonParam);
        }

        var simpleEndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
        var simpleValueParam = Expression.Constant(value);
        return Expression.Call(memberExpression, simpleEndsWithMethod, simpleValueParam);
    }
    
    private static Expression BuildComparisonExpression<TValue>(Expression memberExpression, FilterOperator op, TValue value, bool caseSensitive) {
        switch (op) {
            case FilterOperator.In:
                return BuildInExpression(memberExpression, value?.ToString(), false, caseSensitive);
            case FilterOperator.NotIn:
                return BuildInExpression(memberExpression, value?.ToString(), true, caseSensitive);
            case FilterOperator.Contains:
                return BuildContainsExpression<TValue>(memberExpression, value?.ToString(), true, caseSensitive);
            case FilterOperator.NotContains:
                return BuildContainsExpression<TValue>(memberExpression, value?.ToString(), false, caseSensitive);
            case FilterOperator.StartsWith:
                return BuildStartsWithExpression<TValue>(memberExpression, value?.ToString(), caseSensitive);
            case FilterOperator.EndsWith:
                return BuildEndsWithExpression<TValue>(memberExpression, value?.ToString(), caseSensitive);
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

    /// <summary>
    /// Builds a filter expression using legacy ToLower() approach for backward compatibility.
    /// </summary>
    public static Expression<Func<T,bool>> BuildFilterExpression<T>(string propName, string paramName, FilterOperator op, string? value, bool caseSensitive) {
        var (param, body) = ParameterExpression<T, bool>(propName, paramName);

        // Handle In/NotIn early — the raw comma-separated value would break float.TryParse etc.
        if (op is FilterOperator.In or FilterOperator.NotIn) {
            var inExpr = BuildInExpression(body, value, op == FilterOperator.NotIn, caseSensitive);
            return Expression.Lambda<Func<T, bool>>(inExpr, param);
        }

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

        // Handle In/NotIn early — the raw comma-separated value would break float.TryParse etc.
        if (op is FilterOperator.In or FilterOperator.NotIn) {
            var inExpr = BuildInExpressionWithSettings(body, value, op == FilterOperator.NotIn, caseSensitive, settings);
            return Expression.Lambda<Func<T, bool>>(inExpr, param);
        }

        Expression comparison;
        if (value == null) {
            comparison = BuildComparisonExpressionWithSettings(body, op, value, false, settings);
        }
        else if (float.TryParse(value, out var floatValue)) {
            comparison = BuildComparisonExpressionWithSettings(body, op, floatValue, false, settings);
        }
        else if (bool.TryParse(value, out var boolValue)) {
            comparison = BuildComparisonExpressionWithSettings(body, op, boolValue, false, settings);
        }
        else if (long.TryParse(value, out var longValue)) {
            comparison = BuildComparisonExpressionWithSettings(body, op, longValue, false, settings);
        }
        else {
            comparison = BuildComparisonExpressionWithSettings(body, op, value, caseSensitive, settings);
        }

        return Expression.Lambda<Func<T, bool>>(comparison, param);
    }

    private static Expression BuildComparisonExpressionWithSettings<TValue>(
        Expression memberExpression,
        FilterOperator op,
        TValue value,
        bool caseSensitive,
        PafisoSettings settings) {
        switch (op) {
            case FilterOperator.In:
                return BuildInExpressionWithSettings(memberExpression, value?.ToString(), false, caseSensitive, settings);
            case FilterOperator.NotIn:
                return BuildInExpressionWithSettings(memberExpression, value?.ToString(), true, caseSensitive, settings);
            case FilterOperator.Contains:
                return BuildContainsExpressionWithSettings(memberExpression, value?.ToString(), true, caseSensitive, settings);
            case FilterOperator.NotContains:
                return BuildContainsExpressionWithSettings(memberExpression, value?.ToString(), false, caseSensitive, settings);
            case FilterOperator.StartsWith:
                return BuildStartsWithExpressionWithSettings(memberExpression, value?.ToString(), caseSensitive, settings);
            case FilterOperator.EndsWith:
                return BuildEndsWithExpressionWithSettings(memberExpression, value?.ToString(), caseSensitive, settings);
            case FilterOperator.Null:
                return Expression.ReferenceEqual(memberExpression, Expression.Constant(null));
            case FilterOperator.NotNull:
                return Expression.Not(Expression.ReferenceEqual(memberExpression, Expression.Constant(null)));
        }

        if (memberExpression.Type != typeof(TValue)) {
            memberExpression = Expression.Convert(memberExpression, typeof(TValue));
        }

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
        if (op == FilterOperator.Equals || op == FilterOperator.NotEquals) {
            var equalsMethod = typeof(string).GetMethod(
                nameof(string.Equals),
                [typeof(string), typeof(string), typeof(StringComparison)])!;
            var valueParam = Expression.Constant(value);
            var comparisonParam = Expression.Constant(settings.StringComparison);
            var equalsCall = Expression.Call(null, equalsMethod, memberExpression, valueParam, comparisonParam);
            return op == FilterOperator.Equals ? equalsCall : Expression.Not(equalsCall);
        }

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
}
