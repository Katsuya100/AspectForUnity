using System.ComponentModel;

namespace Katuusagi.AspectForUnity
{
    // Public because generated state types implement it; it is not a user-facing advice API.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProceedingState<T>
    {
        ref T GetReturnValue();
        ref T Proceed(int slot, int generation);
    }
}
