namespace Katuusagi.AspectForUnity.Tests.LocalAssembly
{
    [SimplePointcutDeclaringMarker]
    public sealed class SimplePointcutTarget
    {
        [SimplePointcutMethodMarker]
        public int MatchingMethod<T, U>(string text, int number)
        {
            return number;
        }
    }
}
