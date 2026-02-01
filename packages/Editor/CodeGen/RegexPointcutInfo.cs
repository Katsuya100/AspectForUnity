using Mono.Cecil;
using System.Text.RegularExpressions;

namespace Katuusagi.AspectForUnity.Editor
{
    public class RegexPointcutInfo : IPointcutInfo
    {
        private Regex _regex;
        private PointcutNameFlag _pointcutNameFlag;

        public RegexPointcutInfo(CustomAttribute pointcutRegex)
        {
            var arguments = pointcutRegex.ConstructorArguments;
            _regex = new Regex(arguments[0].Value as string);
            _pointcutNameFlag = (PointcutNameFlag)(ulong)arguments[1].Value;
        }

        public bool IsMatch(MethodReference method)
        {
            var methodName = EditorAspectForUnityUtils.GeneratePointcutMethodName(method, _pointcutNameFlag);
            var result = _regex.IsMatch(methodName);
            return result;
        }
    }
}
