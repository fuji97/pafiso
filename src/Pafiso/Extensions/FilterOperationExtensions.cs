namespace Pafiso.Extensions; 

public static class FilterOperationExtensions {
    public static string ToOperator(this FilterOperator op) {
        return op switch {
            FilterOperator.Equals => "==",
            FilterOperator.NotEquals => "!=",
            FilterOperator.GreaterThan => ">",
            FilterOperator.LessThan => "<",
            FilterOperator.GreaterThanOrEquals => ">=",
            FilterOperator.LessThanOrEquals => "<=",
            FilterOperator.Contains => "contains",
            FilterOperator.NotContains => "not contains",
            FilterOperator.Null => "is null",
            FilterOperator.NotNull => "is not null",
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }
}
