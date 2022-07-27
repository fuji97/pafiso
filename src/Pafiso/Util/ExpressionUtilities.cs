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
}