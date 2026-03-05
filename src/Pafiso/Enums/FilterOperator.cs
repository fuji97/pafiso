using System.Runtime.Serialization;

namespace Pafiso.Enums;

public enum FilterOperator {
    [EnumMember(Value = "eq")]
    Equals,
    [EnumMember(Value = "neq")]
    NotEquals,
    [EnumMember(Value = "gt")]
    GreaterThan,
    [EnumMember(Value = "lt")]
    LessThan,
    [EnumMember(Value = "gte")]
    GreaterThanOrEquals,
    [EnumMember(Value = "lte")]
    LessThanOrEquals,
    [EnumMember(Value = "contains")]
    Contains,
    [EnumMember(Value = "ncontains")]
    NotContains,
    [EnumMember(Value = "null")]
    Null,
    [EnumMember(Value = "notnull")]
    NotNull,
    [EnumMember(Value = "startswith")]
    StartsWith,
    [EnumMember(Value = "endswith")]
    EndsWith,
    [EnumMember(Value = "in")]
    In,
    [EnumMember(Value = "notin")]
    NotIn
}
