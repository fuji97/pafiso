using System.Linq.Expressions;

namespace Pafiso.Util; 

public static class ExpressionUtilities {
    public static string ExpressionDecomposer(Expression expr) {
        if (expr is MemberExpression member) {
            var field = member.Member.Name;

            if (member.Expression is MemberExpression memberExpression) {
                return $"{ExpressionDecomposer(memberExpression)}.{field}";
            }
            
            return field;
        }
        
        if (expr is UnaryExpression unary) {
            return ExpressionDecomposer(unary.Operand);
        }
        
        throw new ArgumentException("Expression must be a member or unary expression");
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
        var propValue = GetPropertyValue(obj, propName);
        if (propValue == null) {
            throw new ArgumentNullException($"Null value: {propName}");
        }
        
        // Cast to long if enum
        if (propValue is Enum) {
            propValue = Convert.ToInt64(propValue);
        }
        
        var value = propValue.ToString();
        if (!caseSensitive) {
            value = value?.ToLower();
        }
        if (value == null) {
            throw new ArgumentNullException($"Null value: {propName}");
        }

        return value;
    }

    /// <summary>
    /// From: https://stackoverflow.com/questions/16208214/construct-lambdaexpression-for-nested-property-from-string
    /// </summary>
    /// <param name="propName"></param>
    /// <param name="paramName"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static Expression<Func<T,TResult>> BuildExpression<T,TResult> (string propName, string paramName = "x") {
        var param = Expression.Parameter(typeof(T), paramName);
        Expression body = param;
        foreach (var member in propName.Split('.')) {
            body = Expression.PropertyOrField(body, member);
        }
        if (body.Type != typeof(TResult)) {
            body = Expression.Convert(body, typeof(TResult));
        }
        return Expression.Lambda<Func<T,TResult>>(body, param);
    }
}