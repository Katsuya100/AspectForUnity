using System;
using System.Collections.Generic;

namespace Katuusagi.AspectForUnity
{
    public static class ProceedingRuntime
    {
        [ThreadStatic]
        private static Dictionary<Type, Stack<ProceedingStateBase>> _states;

        public static object Rent(Type stateType)
        {
            if (_states == null)
            {
                _states = new Dictionary<Type, Stack<ProceedingStateBase>>();
            }

            if (!_states.TryGetValue(stateType, out var states))
            {
                states = new Stack<ProceedingStateBase>();
                _states.Add(stateType, states);
            }

            return states.Count > 0
                ? states.Pop()
                : Activator.CreateInstance(stateType, nonPublic: true);
        }

        public static int Activate(object state)
        {
            return ((ProceedingStateBase)state).Activate();
        }

        public static void EnsureProceed(object state, int slot, int generation)
        {
            ((ProceedingStateBase)state).EnsureProceed(slot, generation);
        }

        public static void Return(object state)
        {
            var proceedingState = (ProceedingStateBase)state;
            proceedingState.Release();
            var stateType = state.GetType();
            if (!_states.TryGetValue(stateType, out var states))
            {
                states = new Stack<ProceedingStateBase>();
                _states.Add(stateType, states);
            }

            states.Push(proceedingState);
        }

        public static ProceedingContext CreateContext(object state, int slot, int generation)
        {
            return new ProceedingContext(state, slot, generation);
        }

        public static ProceedingContext<T> CreateContext<T>(object state, int slot, int generation)
        {
            return new ProceedingContext<T>(state, slot, generation);
        }

        public static UnsafeProceedingContext<T> CreateUnsafeContext<T>(object state, int slot, int generation)
        {
            return new UnsafeProceedingContext<T>(state, slot, generation);
        }

        public static ref readonly T GetReturnValue<T>(ref ProceedingContext<T> context)
        {
            return ref context.ReturnValue;
        }

        public static ref T GetUnsafeReturnValue<T>(ref UnsafeProceedingContext<T> context)
        {
            return ref context.ReturnValue;
        }
    }
}
