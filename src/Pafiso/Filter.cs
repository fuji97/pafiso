﻿using System.Linq.Expressions;
using System.Text.Json.Serialization;
using LinqKit;
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

    public Filter(IEnumerable<string> fields, FilterOperator @operator, string? value, bool caseSensitive = false) {
        Fields = fields.ToList();
        Operator = @operator;
        Value = value;
        CaseSensitive = caseSensitive;
    }

    public Filter(FilterOperator @operator, string? value, bool caseSensitive = false) {
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
        if (expression.Body is BinaryExpression binaryExpression) {
            var field = ExpressionUtilities.ExpressionDecomposer(binaryExpression.Left);

            string? value;
            try {
                value = ExpressionUtilities.GetExpressionValue(binaryExpression.Right);
            }
            catch (InvalidOperationException e) {
                throw new InvalidOperationException($"Expression must be a binary expression with a constant value on the right side. {e.Message}");
            }
        
            var operatorName = binaryExpression.NodeType.ToFilterOperator(value);
        
            return new Filter(field, operatorName, value);
        } 
        
        if (expression.Body is UnaryExpression unaryExpression) {
            var (path, op, value) = ExpressionUtilities.DecomposeUnaryWrapperExpression(unaryExpression);
            return new Filter(path, op, value);
        }
        
        if (expression.Body is MethodCallExpression methodCallExpression) {
            var (path, op, value) = ExpressionUtilities.DecomposeMethodCallExpression(methodCallExpression);
            return new Filter(path, op, value);
        }
        
        throw new InvalidOperationException("Unsupported expression");
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
    
    public Filter AddField<T>(Expression<Func<T, object>> fieldExpression) {
        var member = fieldExpression.Body as MemberExpression;
        if (member == null) {
            throw new InvalidOperationException("Expression must be a member expression");
        }
        var field = ExpressionUtilities.ExpressionDecomposer(member);
        Fields.Add(field);
        return this;
    }
    
    public IQueryable<T> ApplyFilter<T>(IQueryable<T> query) {
        var predicatesBuilder = PredicateBuilder.New<T>();

        foreach (var field in Fields) {
            predicatesBuilder.Or(ApplyCorrectOperation<T>(this, field));
        }

        return query.Where(predicatesBuilder);
    }
    
    // private Expression<Func<T,bool>> ApplyCorrectOperation<T>(Filter filter, string field, string? value) {
    //     switch (filter.Operator) {
    //         case FilterOperator.Equals:
    //             return (x => ExpressionUtilities.GetStringPropertyValue(x, field, filter.CaseSensitive) == value);
    //         case FilterOperator.NotEquals:
    //             return (x => ExpressionUtilities.GetStringPropertyValue(x, field, filter.CaseSensitive) != value);
    //         case FilterOperator.GreaterThan:
    //             return (x => float.Parse(ExpressionUtilities.GetStringPropertyValue(x, field, true)) > float.Parse(value!));    // TODO Correct null handling
    //         case FilterOperator.LessThan:
    //             return (x => float.Parse(ExpressionUtilities.GetStringPropertyValue(x, field, true)) < float.Parse(value!));   // TODO Correct null handling
    //         case FilterOperator.GreaterThanOrEquals:
    //             return (x => float.Parse(ExpressionUtilities.GetStringPropertyValue(x, field, true)) >= float.Parse(value!));    // TODO Correct null handling
    //         case FilterOperator.LessThanOrEquals:
    //             return (x => float.Parse(ExpressionUtilities.GetStringPropertyValue(x, field, true)) <= float.Parse(value!));   // TODO Correct null handling
    //         case FilterOperator.Contains:
    //             return (x => ExpressionUtilities.GetStringPropertyValue(x, field, filter.CaseSensitive).Contains(value!));  // TODO Correct null handling
    //         case FilterOperator.NotContains:
    //             return (x => !ExpressionUtilities.GetStringPropertyValue(x, field, filter.CaseSensitive).Contains(value!)); // TODO Correct null handling
    //         case FilterOperator.Null:
    //             return (x => ExpressionUtilities.GetPropertyValue(x, field) == null);
    //         case FilterOperator.NotNull:
    //             return (x => ExpressionUtilities.GetPropertyValue(x, field) != null);
    //         default:
    //             throw new ArgumentOutOfRangeException();
    //     }
    // }
    
    private Expression<Func<T,bool>> ApplyCorrectOperation<T>(Filter filter, string field) {
        return ExpressionUtilities.BuildFilterExpression<T>(field, "x", filter.Operator, filter.Value, filter.CaseSensitive);
    }

    public override string ToString() {
        return $"({string.Join(" OR ", Fields.Select(field => $"{field} {Operator} {Value}"))})";
    }
}

public class Filter<T> : Filter {
    public Filter() {
    }

    public Filter(string field, FilterOperator @operator, string? value, bool caseSensitive = false) : base(field, @operator, value, caseSensitive) {
    }

    public Filter(IEnumerable<string> fields, FilterOperator @operator, string? value, bool caseSensitive = false) : base(fields, @operator, value, caseSensitive) {
    }

    public Filter(FilterOperator @operator, string? value, bool caseSensitive = false) : base(@operator, value, caseSensitive) {
    }

    public Filter<T> AddField(Expression<Func<T,object>> fieldExpression) {
        AddField<T>(fieldExpression);
        return this;
    }

    public static Filter<T> FromExpression(Expression<Func<T,bool>> expression) {
        var genericFilter = Filter.FromExpression(expression);
        return new Filter<T>(genericFilter.Fields, genericFilter.Operator, genericFilter.Value, genericFilter.CaseSensitive);
    }
}