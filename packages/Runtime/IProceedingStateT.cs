namespace Katuusagi.AspectForUnity
{
    // This interface is consumed by generated IL; it is not exposed by advice signatures.
    public interface IProceedingState<T>
    {
        ref T GetReturnValue();
        ref T Proceed(int slot, int generation);
    }
}
