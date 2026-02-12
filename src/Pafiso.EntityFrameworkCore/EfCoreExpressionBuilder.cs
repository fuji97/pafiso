using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Pafiso.Util;

namespace Pafiso.EntityFrameworkCore;

/// <summary>
/// Provides EF Core-specific expression building capabilities for Pafiso.
/// </summary>
public sealed class EfCoreExpressionBuilder : IFilterExpressionBuilder {
    public static EfCoreExpressionBuilder Instance { get; } = new();

    private EfCoreExpressionBuilder() { }

    /// <summary>
    /// Builds an expression for EF.Functions.Like.
    /// </summary>
    /// <param name="memberExpression">The member expression representing the property to compare.</param>
    /// <param name="pattern">The LIKE pattern (e.g., "%value%").</param>
    /// <returns>An expression representing the LIKE comparison.</returns>
    public static Expression BuildLikeExpression(Expression memberExpression, string pattern) {
        // Get the EF.Functions property
        var efFunctionsProperty = typeof(EF).GetProperty(
            nameof(EF.Functions),
            BindingFlags.Public | BindingFlags.Static)!;

        // Get the DbFunctions instance
        var efFunctionsExpr = Expression.Property(null, efFunctionsProperty);

        // Get the Like method: EF.Functions.Like(string, string)
        var likeMethod = typeof(DbFunctionsExtensions).GetMethod(
            nameof(DbFunctionsExtensions.Like),
            [typeof(DbFunctions), typeof(string), typeof(string)])!;

        // Ensure the member expression is a string
        if (memberExpression.Type != typeof(string)) {
            memberExpression = Expression.Convert(memberExpression, typeof(string));
        }

        // Build the call: EF.Functions.Like(member, pattern)
        var patternExpr = Expression.Constant(pattern);
        return Expression.Call(null, likeMethod, efFunctionsExpr, memberExpression, patternExpr);
    }

    public Expression<Func<T, bool>> BuildFilterExpression<T>(
        string propName,
        string paramName,
        FilterOperator op,
        string? value,
        bool caseSensitive,
        PafisoSettings settings) {
        return ExpressionUtilities.BuildFilterExpression<T>(
            propName,
            paramName,
            op,
            value,
            caseSensitive,
            settings,
            BuildLikeExpression);
    }
}
