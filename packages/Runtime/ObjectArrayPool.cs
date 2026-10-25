using System.Buffers;
using System.ComponentModel;

namespace Katuusagi.AspectForUnity
{
    // Public because generated IL rents and returns argument arrays through it; direct user use is not intended.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ObjectArrayPool
    {
        public static object[] Rent(int length)
        {
            return ArrayPool<object>.Shared.Rent(length);
        }

        public static void Return(object[] array)
        {
            ArrayPool<object>.Shared.Return(array, clearArray: true);
        }
    }
}
