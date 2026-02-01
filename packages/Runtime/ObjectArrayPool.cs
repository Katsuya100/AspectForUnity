using System.Buffers;

namespace Katuusagi.AspectForUnity
{
    public static class ObjectArrayPool
    {
        public static object[] Rent(int length)
        {
            return ArrayPool<object>.Shared.Rent(length);
        }

        public static void Return(object[] array)
        {
            ArrayPool<object>.Shared.Return(array);
        }
    }
}
