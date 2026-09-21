using System;
using Mono.Cecil;

namespace Katuusagi.AspectForUnity.Editor
{
    internal sealed class GenericParameterNamePointcutInfo : IPointcutInfo
    {
        private readonly bool _hasRepeatedIndex;
        private readonly int _repeatedIndex;
        private readonly string _name;

        public GenericParameterNamePointcutInfo(CustomAttribute pointcut)
        {
            _hasRepeatedIndex = pointcut.ConstructorArguments.Count > 1;
            if (_hasRepeatedIndex)
            {
                _repeatedIndex = Convert.ToInt32(pointcut.ConstructorArguments[0].Value);
                _name = pointcut.ConstructorArguments[1].Value as string;
            }
            else
            {
                _name = pointcut.ConstructorArguments[0].Value as string;
            }
        }

        public bool IsMatch(MethodReference method)
        {
            return _hasRepeatedIndex
                ? SimplePointcutInfoUtils.IsRepeatedMatch(method.GenericParameters,
                                                          _repeatedIndex,
                                                          v => v.Name == _name)
                : SimplePointcutInfoUtils.IsAnyMatch(method.GenericParameters,
                                                     v => v.Name == _name);
        }
    }
}
