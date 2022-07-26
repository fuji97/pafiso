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
}