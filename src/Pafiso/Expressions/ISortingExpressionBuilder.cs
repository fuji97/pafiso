using System.Linq.Expressions;

namespace Pafiso;

public interface ISortingExpressionBuilder {
    Expression<Func<T, object>> BuildSortingExpression<T>(string propName, string paramName = "x");
}
