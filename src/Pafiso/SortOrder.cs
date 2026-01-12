using System.Runtime.Serialization;

namespace Pafiso;

public enum SortOrder {
    [EnumMember(Value = "asc")]
    Ascending,
    [EnumMember(Value = "desc")]
    Descending
}
