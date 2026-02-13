using System.Linq.Expressions;

namespace Pafiso.Expressions;

public interface ISortingExpressionBuilder {
    Expression<Func<T, object>> BuildSortingExpression<T>(string propName, string paramName = "x");
}
