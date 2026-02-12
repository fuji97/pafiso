using System.Linq.Expressions;
using Pafiso.Util;

namespace Pafiso;

public sealed class DefaultFilterExpressionBuilder : IFilterExpressionBuilder {
    public static DefaultFilterExpressionBuilder Instance { get; } = new();

    private DefaultFilterExpressionBuilder() { }

    public Expression<Func<T, bool>> BuildFilterExpression<T>(
        string propName,
        string paramName,
        FilterOperator op,
        string? value,
        bool caseSensitive,
        PafisoSettings settings) {
        return ExpressionUtilities.BuildFilterExpression<T>(propName, paramName, op, value, caseSensitive, settings);
    }
}
