using Mono.Cecil;
using UnityEngine;

namespace Katuusagi.AspectForUnity.Editor
{
    public interface IPointcutInfo
    {
        bool IsMatch(MethodReference method);
    }
}
