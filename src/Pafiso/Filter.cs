using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Pafiso.Util;

namespace Pafiso; 

public class Filter {
    public List<string> Fields { get; } = new();
    public FilterOperator Operator { get; }
    public string? Value { get; } = null!;
    public bool CaseSensitive { get; } = false;

    public Filter() {
    }

    [JsonConstructor]
    public Filter(string field, FilterOperator @operator, string? value, bool caseSensitive = false) {
        Fields = new List<string> { field };
        Operator = @operator;
        Value = value;
        CaseSensitive = caseSensitive;
    }

    public Filter(IEnumerable<string> fields, FilterOperator @operator, string? value, bool caseSensitive) {
        Fields = fields.ToList();
        Operator = @operator;
        Value = value;
        CaseSensitive = caseSensitive;
    }

    public bool Equals(Filter other) {
        return Fields.SequenceEqual(other.Fields) && Operator == other.Operator && Value == other.Value && CaseSensitive == other.CaseSensitive;
    }

    public override bool Equals(object? obj) {
        return obj is Filter other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Fields, (int)Operator, Value, CaseSensitive);
    }

    public static bool operator ==(Filter left, Filter right) {
        return left.Equals(right);
    }

    public static bool operator !=(Filter left, Filter right) {
        return !left.Equals(right);
    }
    
    public static Filter FromExpression<T>(Expression<Func<T,bool>> expression) {
        var binaryExpression = expression.Body as BinaryExpression;
        if (binaryExpression == null) {
            throw new InvalidOperationException("Expression must be a binary expression");
        }
        var left = binaryExpression.Left as MemberExpression;
        if (left == null) {
            throw new InvalidOperationException("Expression must be a binary expression");
        }

        string? value;
        if (binaryExpression.Right is ConstantExpression constantExpression) {
            value = constantExpression.Value?.ToString();
        } else if (binaryExpression.Right is MemberExpression rightMember) {
            value = ExpressionUtilities.GetValue(rightMember).ToString();
        } else {
            throw new InvalidOperationException("Expression must be a binary expression");
        }
        
        var field = ExpressionUtilities.MemberDecomposer(left);
        var operatorType = binaryExpression.NodeType;
        var operatorName = operatorType switch {
            ExpressionType.Equal => FilterOperator.Equals,
            ExpressionType.NotEqual => FilterOperator.NotEquals,
            ExpressionType.GreaterThan => FilterOperator.GreaterThan,
            ExpressionType.GreaterThanOrEqual => FilterOperator.GreaterThanOrEquals,
            ExpressionType.LessThan => FilterOperator.LessThan,
            ExpressionType.LessThanOrEqual => FilterOperator.LessThanOrEquals,
            _ => throw new InvalidOperationException("Expression must be a binary expression")
        };
        
        // Convert to null check if value is null and operator is equals or not equals
        if (value == null) {
            operatorName = operatorName switch {
                FilterOperator.Equals => FilterOperator.Null,
                FilterOperator.NotEquals => FilterOperator.NotNull,
                _ => operatorName
            };
        }
        return new Filter(field, operatorName, value);
    }

    public IDictionary<string, string> ToDictionary() {
        var dict = new Dictionary<string, string>() {
            ["fields"] = string.Join(',', Fields),
            ["op"] = Operator.ToString(),
        };
        if (Value != null) {
            dict["val"] = Value;
        }
        if (CaseSensitive) {
            dict["case"] = "true";
        }

        return dict;
    }
    
    public static Filter FromDictionary(IDictionary<string, string> dict) {
        var fields = dict["fields"]!.Split(",");
        var op = dict["op"]!;
        dict.TryGetValue("val", out var val);
        var caseSensitive = dict.ContainsKey("case") && dict["case"] == "true";
        return new Filter(fields, Enum.Parse<FilterOperator>(op), val, caseSensitive);
    }
}