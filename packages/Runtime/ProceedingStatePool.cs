using System.Collections.Generic;

namespace Katuusagi.AspectForUnity
{
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
