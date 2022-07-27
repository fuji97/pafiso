using System.Linq.Expressions;

namespace Pafiso.Util; 

public static class ExpressionUtilities {
    public static string MemberDecomposer(MemberExpression member) {
        var field = member.Member.Name;
        
        if (member.Expression is MemberExpression expression) {
            return $"{MemberDecomposer(expression)}.{field}";
        }

        return field;
    }
    
    public static object GetValue(MemberExpression member) {
        var objectMember = Expression.Convert(member, typeof(object));
        var getterLambda = Expression.Lambda<Func<object>>(objectMember);
        var getter = getterLambda.Compile();
        return getter();
    }
    
    /// <summary>
    /// Obtain value from nester property values.
    /// https://stackoverflow.com/questions/1954746/using-reflection-in-c-sharp-to-get-properties-of-a-nested-object
    /// </summary>
    /// <param name="src"></param>
    /// <param name="propName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static object? GetPropertyValue(object? src, string propName)
    {
        //if (src == null) throw new ArgumentException("Value cannot be null.", "src");
        if (src == null) return null;
        if (propName == null) throw new ArgumentException("Value cannot be null.", nameof(propName));

        if(propName.Contains('.')) //complex type nested
        {
            var temp = propName.Split(new char[] { '.' }, 2);
            return GetPropertyValue(GetPropertyValue(src, temp[0]), temp[1]);
        }
        else {
            var prop = src.GetType().GetProperty(propName);
            return prop != null ? prop.GetValue(src, null) : null;
        }
    }
    
    public static string GetStringPropertyValue<T>(T obj, string propName, bool caseSensitive) {
        var value = GetPropertyValue(obj, propName)?.ToString();
        if (!caseSensitive) {
            value = value?.ToLower();
        }
        if (value == null) {
            throw new ArgumentNullException($"Null value: {propName}");
        }

        return value;
    }
}