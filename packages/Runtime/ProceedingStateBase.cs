using System;
using System.Collections.Generic;

namespace Katuusagi.AspectForUnity
{
    public abstract class ProceedingStateBase
    {
        private readonly HashSet<int> _proceededSlots = new HashSet<int>();
        private int _generation;
        private bool _active;
        private bool _poisoned;

        public int Activate()
        {
            unchecked
            {
                ++_generation;
                if (_generation == 0)
                {
                    _generation = 1;
                }
            }

            _active = true;
            _poisoned = false;
            _proceededSlots.Clear();
            return _generation;
        }

        public void Release()
        {
            _active = false;
            _proceededSlots.Clear();
            ClearState();
        }

        protected virtual void ClearState()
        {
        }

        protected void BeginProceed(int slot, int generation)
        {
            Validate(generation);
            if (_poisoned)
            {
                throw new InvalidOperationException("Proceed cannot be called after a previous Proceed failed.");
            }

            if (!_proceededSlots.Add(slot))
            {
                throw new InvalidOperationException("Proceed may only be called once for an Around context.");
            }
        }

        protected void FailProceed(int generation)
        {
            Validate(generation);
            _poisoned = true;
        }

        public void EnsureProceed(int slot, int generation)
        {
            Validate(generation);
            if (!_proceededSlots.Contains(slot))
            {
                throw new InvalidOperationException("Around advice must call Proceed before it returns.");
            }
        }

        private void Validate(int generation)
        {
            if (!_active || generation != _generation)
            {
                throw new InvalidOperationException("The ProceedingContext is no longer valid.");
            }
        }
    }
}
