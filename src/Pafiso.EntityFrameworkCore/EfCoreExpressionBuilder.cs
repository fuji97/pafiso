using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Pafiso.Util;

namespace Pafiso.EntityFrameworkCore;

/// <summary>
/// Provides EF Core-specific expression building capabilities for Pafiso.
/// </summary>
public static class EfCoreExpressionBuilder {
    /// <summary>
    /// Registers the EF Core Like expression builder with Pafiso.
    /// This should be called during application startup.
    /// </summary>
    public static void Register() {
        // Use the delegate itself as the registration check - if it's already set to our method, skip
        if (ExpressionUtilities.EfCoreLikeExpressionBuilder == BuildLikeExpression) return;

        ExpressionUtilities.EfCoreLikeExpressionBuilder = BuildLikeExpression;
    }

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
}
