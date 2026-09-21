using System;
using System.Runtime.InteropServices;

namespace Katuusagi.AspectForUnity
{
    public ref struct UnsafeProceedingContext<T>
    {
        private readonly IProceedingState<T> _state;
        private readonly int _slot;
        private readonly int _generation;
        private Span<T> _returnReference;
        private readonly bool _hasReturnReference;

        public UnsafeProceedingContext(object state, int slot, int generation)
        {
            _state = state as IProceedingState<T> ?? throw new ArgumentException("Invalid UnsafeProceedingContext state.", nameof(state));
            _slot = slot;
            _generation = generation;
            _returnReference = default;
            _hasReturnReference = false;
        }

        public UnsafeProceedingContext(object state, int slot, int generation, ref T initialReturnValue)
        {
            _state = state as IProceedingState<T> ?? throw new ArgumentException("Invalid UnsafeProceedingContext state.", nameof(state));
            _slot = slot;
            _generation = generation;
            _returnReference = MemoryMarshal.CreateSpan(ref initialReturnValue, 1);
            _hasReturnReference = true;
        }

        public void Proceed()
        {
            ref T result = ref _state.Proceed(_slot, _generation);
            if (_hasReturnReference)
            {
                _returnReference = MemoryMarshal.CreateSpan(ref result, 1);
            }
        }

        public ref T ReturnValue
        {
            get
            {
                if (_hasReturnReference)
                {
                    return ref _returnReference[0];
                }

                return ref _state.GetReturnValue();
            }
        }
    }
}
