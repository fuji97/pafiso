using System.Linq.Expressions;
using System.Reflection;
using Pafiso.Enums;
using Pafiso.Util;

namespace Pafiso.EntityFrameworkCore;

internal static class EfCoreExpressionUtilities {
    private static readonly MethodInfo ToLowerMethod =
        typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
    internal static Expression<Func<T, bool>> BuildFilterExpression<T>(
        string propName,
        string paramName,
        FilterOperator op,
        string? value,
        bool caseSensitive,
        PafisoSettings settings,
        Func<Expression, string, Expression> likeExpressionBuilder) {
        var (param, body) = ParameterExpression<T>(propName, paramName);

        // Handle In/NotIn early — the raw comma-separated value would break float.TryParse etc.
        if (op is FilterOperator.In or FilterOperator.NotIn) {
            var inExpr = BuildInExpression(body, value, op == FilterOperator.NotIn, caseSensitive);
            return Expression.Lambda<Func<T, bool>>(inExpr, param);
        }

        Expression comparison;
        if (value == null) {
            comparison = BuildComparisonExpressionWithLike(body, op, value, false, settings, likeExpressionBuilder);
        }
        else if (float.TryParse(value, out var floatValue)) {
            comparison = BuildComparisonExpressionWithLike(body, op, floatValue, false, settings, likeExpressionBuilder);
        }
        else if (bool.TryParse(value, out var boolValue)) {
            comparison = BuildComparisonExpressionWithLike(body, op, boolValue, false, settings, likeExpressionBuilder);
        }
        else if (long.TryParse(value, out var longValue)) {
            comparison = BuildComparisonExpressionWithLike(body, op, longValue, false, settings, likeExpressionBuilder);
        }
        else {
            comparison = BuildComparisonExpressionWithLike(body, op, value, caseSensitive, settings, likeExpressionBuilder);
        }

        return Expression.Lambda<Func<T, bool>>(comparison, param);
    }

    private static (ParameterExpression param, Expression body) ParameterExpression<T>(string propName, string paramName) {
        var param = Expression.Parameter(typeof(T), paramName);
        Expression body = param;
        foreach (var member in propName.Split('.')) {
            body = Expression.PropertyOrField(body, member);
        }
        return (param, body);
    }

    private static Expression BuildInExpression(Expression memberExpression, string? value, bool negate, bool caseSensitive) {
        if (value == null) {
            return Expression.Constant(false);
        }

        var items = ExpressionUtilities.SplitEscapedValues(value);

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

        // Default: string — use ToLowerInvariant for values, ToLower() for member (EF Core pattern)
        {
            var parsed = caseSensitive ? items : items.Select(s => s.ToLowerInvariant()).ToList();
            var listExpr = Expression.Constant(parsed);
            if (memberExpression.Type != typeof(string)) {
                memberExpression = Expression.Convert(memberExpression, typeof(string));
            }
            if (!caseSensitive) {
                memberExpression = Expression.Call(memberExpression, ToLowerMethod);
            }
            var containsMethod = typeof(List<string>).GetMethod(nameof(List<string>.Contains), [typeof(string)])!;
            var call = Expression.Call(listExpr, containsMethod, memberExpression);
            return negate ? Expression.Not(call) : (Expression)call;
        }
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_")
            .Replace("[", @"\[");
    }

    private static Expression BuildContainsExpressionWithLike(
        Expression memberExpression,
        string? value,
        bool contains,
        bool caseSensitive,
        Func<Expression, string, Expression> likeExpressionBuilder) {
        if (value == null) {
            return Expression.Constant(false);
        }

        if (memberExpression.Type != typeof(string)) {
            memberExpression = Expression.Convert(memberExpression, typeof(string));
        }

        if (!caseSensitive) {
            var lowerMember = Expression.Call(memberExpression, ToLowerMethod);
            var pattern = $"%{EscapeLikePattern(value.ToLowerInvariant())}%";
            var likeExpression = likeExpressionBuilder(lowerMember, pattern);
            return contains ? likeExpression : Expression.Not(likeExpression);
        }

        var simpleContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var simpleValueParam = Expression.Constant(value);
        var simpleMethodCall = Expression.Call(memberExpression, simpleContainsMethod, simpleValueParam);
        return contains ? simpleMethodCall : Expression.Not(simpleMethodCall);
    }

    private static Expression BuildStartsWithExpressionWithLike(
        Expression memberExpression,
        string? value,
        bool caseSensitive,
        Func<Expression, string, Expression> likeExpressionBuilder) {
        if (value == null) {
            return Expression.Constant(false);
        }

        if (memberExpression.Type != typeof(string)) {
            memberExpression = Expression.Convert(memberExpression, typeof(string));
        }

        if (!caseSensitive) {
            var lowerMember = Expression.Call(memberExpression, ToLowerMethod);
            var pattern = $"{EscapeLikePattern(value.ToLowerInvariant())}%";
            return likeExpressionBuilder(lowerMember, pattern);
        }

        var simpleStartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
        var simpleValueParam = Expression.Constant(value);
        return Expression.Call(memberExpression, simpleStartsWithMethod, simpleValueParam);
    }

    private static Expression BuildEndsWithExpressionWithLike(
        Expression memberExpression,
        string? value,
        bool caseSensitive,
        Func<Expression, string, Expression> likeExpressionBuilder) {
        if (value == null) {
            return Expression.Constant(false);
        }

        if (memberExpression.Type != typeof(string)) {
            memberExpression = Expression.Convert(memberExpression, typeof(string));
        }

        if (!caseSensitive) {
            var lowerMember = Expression.Call(memberExpression, ToLowerMethod);
            var pattern = $"%{EscapeLikePattern(value.ToLowerInvariant())}";
            return likeExpressionBuilder(lowerMember, pattern);
        }

        var simpleEndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
        var simpleValueParam = Expression.Constant(value);
        return Expression.Call(memberExpression, simpleEndsWithMethod, simpleValueParam);
    }

    private static Expression BuildComparisonExpressionWithLike<TValue>(
        Expression memberExpression,
        FilterOperator op,
        TValue value,
        bool caseSensitive,
        PafisoSettings settings,
        Func<Expression, string, Expression> likeExpressionBuilder) {
        switch (op) {
            case FilterOperator.Contains:
                return BuildContainsExpressionWithLike(memberExpression, value?.ToString(), true, caseSensitive, likeExpressionBuilder);
            case FilterOperator.NotContains:
                return BuildContainsExpressionWithLike(memberExpression, value?.ToString(), false, caseSensitive, likeExpressionBuilder);
            case FilterOperator.StartsWith:
                return BuildStartsWithExpressionWithLike(memberExpression, value?.ToString(), caseSensitive, likeExpressionBuilder);
            case FilterOperator.EndsWith:
                return BuildEndsWithExpressionWithLike(memberExpression, value?.ToString(), caseSensitive, likeExpressionBuilder);
            case FilterOperator.Null:
                return Expression.ReferenceEqual(memberExpression, Expression.Constant(null));
            case FilterOperator.NotNull:
                return Expression.Not(Expression.ReferenceEqual(memberExpression, Expression.Constant(null)));
        }

        if (memberExpression.Type != typeof(TValue)) {
            memberExpression = Expression.Convert(memberExpression, typeof(TValue));
        }

        if (typeof(TValue) == typeof(string) && !caseSensitive) {
            return BuildStringComparisonExpressionWithLike(memberExpression, op, value?.ToString(), settings, likeExpressionBuilder);
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

    private static Expression BuildStringComparisonExpressionWithLike(
        Expression memberExpression,
        FilterOperator op,
        string? value,
        PafisoSettings settings,
        Func<Expression, string, Expression> likeExpressionBuilder) {
        switch (op) {
            case FilterOperator.Equals: {
                var lowerMember = Expression.Call(memberExpression, ToLowerMethod);
                var pattern = EscapeLikePattern((value ?? "").ToLowerInvariant());
                return likeExpressionBuilder(lowerMember, pattern);
            }
            case FilterOperator.NotEquals: {
                var lowerMember = Expression.Call(memberExpression, ToLowerMethod);
                var pattern = EscapeLikePattern((value ?? "").ToLowerInvariant());
                return Expression.Not(likeExpressionBuilder(lowerMember, pattern));
            }
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
