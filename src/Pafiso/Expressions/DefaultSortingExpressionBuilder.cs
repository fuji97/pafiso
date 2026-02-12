using System.Linq.Expressions;
using Pafiso.Util;

namespace Pafiso;

public sealed class DefaultSortingExpressionBuilder : ISortingExpressionBuilder {
    public static DefaultSortingExpressionBuilder Instance { get; } = new();

    private DefaultSortingExpressionBuilder() { }

    public Expression<Func<T, object>> BuildSortingExpression<T>(string propName, string paramName = "x") {
        return ExpressionUtilities.BuildLambdaExpression<T, object>(propName, paramName);
    }
}
