using System.Linq.Expressions;
using Pafiso.Enums;

namespace Pafiso.Expressions;

public interface IFilterExpressionBuilder {
    Expression<Func<T, bool>> BuildFilterExpression<T>(
        string propName,
        string paramName,
        FilterOperator op,
        string? value,
        bool caseSensitive,
        PafisoSettings settings);
}
