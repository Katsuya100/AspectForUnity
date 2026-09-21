namespace Katuusagi.AspectForUnity
{
    public ref struct ProceedingContext
    {
        private readonly IProceedingState _state;
        private readonly int _slot;
        private readonly int _generation;

        public ProceedingContext(object state, int slot, int generation)
        {
            _state = state as IProceedingState ?? throw new System.ArgumentException("Invalid ProceedingContext state.", nameof(state));
            _slot = slot;
            _generation = generation;
        }

        public void Proceed()
        {
            _state.Proceed(_slot, _generation);
        }
    }
}
