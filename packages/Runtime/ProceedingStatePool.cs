using System.Collections.Generic;
using System.ComponentModel;

namespace Katuusagi.AspectForUnity
{
    // Kept public for runtime compatibility; generated IL does not call this helper and direct user use is not intended.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ProceedingStatePool<T>
        where T : ProceedingStateBase, new()
    {
        [System.ThreadStatic]
        private static Stack<T> _states;

        public static T Rent()
        {
            if (_states == null)
            {
                _states = new Stack<T>();
            }

            return _states.Count > 0 ? _states.Pop() : new T();
        }

        public static void Return(T state)
        {
            state.Release();
            _states.Push(state);
        }
    }
}
