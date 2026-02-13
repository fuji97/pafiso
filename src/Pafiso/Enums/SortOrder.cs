using System.Runtime.Serialization;

namespace Pafiso.Enums;

public enum SortOrder {
    [EnumMember(Value = "asc")]
    Ascending,
    [EnumMember(Value = "desc")]
    Descending
}
