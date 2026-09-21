using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace Katuusagi.AspectForUnity.Editor
{
    internal static class SimplePointcutInfoUtils
    {
        public static IEnumerable<TypeDefinition> GetDeclaringTypes(MethodReference method)
        {
            var declaringType = method.DeclaringType;
            while (declaringType != null)
            {
                var typeDefinition = declaringType.Resolve();
                if (typeDefinition == null)
                {
                    yield break;
                }

                yield return typeDefinition;
                declaringType = typeDefinition.DeclaringType;
            }
        }

        public static bool IsRepeatedMatch<T>(IList<T> values,
                                              int repeatedIndex,
                                              Func<T, bool> predicate)
        {
            if (values.Count <= 0)
            {
                return false;
            }

            var index = repeatedIndex % values.Count;
            if (index < 0)
            {
                index += values.Count;
            }

            return predicate(values[index]);
        }

        public static bool IsAnyMatch<T>(IList<T> values,
                                        Func<T, bool> predicate)
        {
            foreach (var value in values)
            {
                if (predicate(value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
