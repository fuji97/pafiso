using System.Reflection;
using System.Runtime.Serialization;

namespace Pafiso.Extensions;

public static class EnumExtensions {
    public static string ToEnumMemberValue<T>(this T value) where T : Enum {
        var memberInfo = typeof(T).GetMember(value.ToString()).FirstOrDefault();
        var attribute = memberInfo?.GetCustomAttribute<EnumMemberAttribute>();
        return attribute?.Value ?? value.ToString();
    }

    public static T ParseEnumMember<T>(string value) where T : struct, Enum {
        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            var attribute = field.GetCustomAttribute<EnumMemberAttribute>();
            if (attribute?.Value == value) {
                return (T)field.GetValue(null)!;
            }
        }
        return Enum.Parse<T>(value);
    }
}
