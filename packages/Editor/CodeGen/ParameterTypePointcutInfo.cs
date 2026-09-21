using System;
using Mono.Cecil;

namespace Katuusagi.AspectForUnity.Editor
{
    internal sealed class ParameterTypePointcutInfo : TypeNamePointcutInfo
    {
        private readonly bool _hasRepeatedIndex;
        private readonly int _repeatedIndex;

        public ParameterTypePointcutInfo(CustomAttribute pointcut)
            : base(pointcut, pointcut.ConstructorArguments.Count > 1 ? 1 : 0)
        {
            _hasRepeatedIndex = pointcut.ConstructorArguments.Count > 1;
            if (_hasRepeatedIndex)
            {
                _repeatedIndex = Convert.ToInt32(pointcut.ConstructorArguments[0].Value);
            }
        }

        public override bool IsMatch(MethodReference method)
        {
            return _hasRepeatedIndex
                ? SimplePointcutInfoUtils.IsRepeatedMatch(method.Parameters,
                                                          _repeatedIndex,
                                                          v => IsTypeMatch(v.ParameterType, TypeName))
                : SimplePointcutInfoUtils.IsAnyMatch(method.Parameters,
                                                     v => IsTypeMatch(v.ParameterType, TypeName));
        }
    }
}
