using System.Linq.Expressions;
using Pafiso.Enums;

namespace Pafiso.EntityFrameworkCore;

internal static class EfCoreExpressionUtilities {
    internal static Expression<Func<T, bool>> BuildFilterExpression<T>(
        string propName,
        string paramName,
        FilterOperator op,
        string? value,
        bool caseSensitive,
        PafisoSettings settings,
        Func<Expression, string, Expression> likeExpressionBuilder) {
        var (param, body) = ParameterExpression<T>(propName, paramName);

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

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
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
            var lowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
            var lowerMember = Expression.Call(memberExpression, lowerMethod);
            var pattern = $"%{EscapeLikePattern(value.ToLower())}%";
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
            var lowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
            var lowerMember = Expression.Call(memberExpression, lowerMethod);
            var pattern = $"{EscapeLikePattern(value.ToLower())}%";
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
            var lowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
            var lowerMember = Expression.Call(memberExpression, lowerMethod);
            var pattern = $"%{EscapeLikePattern(value.ToLower())}";
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
        var lowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var lowerMember = Expression.Call(memberExpression, lowerMethod);
        switch (op) {
            case FilterOperator.Equals: {
                var pattern = EscapeLikePattern((value ?? "").ToLower());
                return likeExpressionBuilder(lowerMember, pattern);
            }
            case FilterOperator.NotEquals: {
                var pattern = EscapeLikePattern((value ?? "").ToLower());
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
