using System.ComponentModel;

namespace Katuusagi.AspectForUnity
{
    // Public because generated state types implement it; it is not a user-facing advice API.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProceedingState
    {
        void Proceed(int slot, int generation);
    }
}
