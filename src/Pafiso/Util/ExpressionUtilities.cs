using System.Linq.Expressions;

namespace Pafiso.Util; 

public static class ExpressionUtilities {
    public static string ExpressionDecomposer(Expression expr) {
        if (expr is MemberExpression member) {
            var field = member.Member.Name;

            if (member.Expression is MemberExpression memberExpression) {
                return $"{ExpressionDecomposer(memberExpression)}.{field}";
            }
            
            return field;
        }
        
        if (expr is UnaryExpression unary) {
            return ExpressionDecomposer(unary.Operand);
        }
        
        throw new ArgumentException("Expression must be a member or unary expression");
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
        string? value;
        
        if (expr is ConstantExpression constantExpression) {
            value = constantExpression.Value?.ToString();
        } else if (expr is MemberExpression rightMember) {
            value = GetValue(rightMember).ToString();
        } else {
            throw new InvalidOperationException("Invalid expression");
        }

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
            var temp = propName.Split(new char[] { '.' }, 2);
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
        
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
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
}