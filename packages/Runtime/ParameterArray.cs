using System;
using System.Collections;

namespace Katuusagi.AspectForUnity
{
    public readonly ref struct ParameterArray
    {
        private readonly object[] _parameters;
        private readonly int _length;
        public int Length => _length;
        public object this[int index]
        {
            get
            {
                if (_length < 0 || (uint)index >= (uint)_length || _parameters == null)
                {
                    throw new IndexOutOfRangeException($"invalid index: {index}");
                }

                return _parameters[index];
            }
        }

        public ParameterArray(int length, object[] parameters)
        {
            _length = length;
            _parameters = parameters;
        }

        public IEnumerator GetEnumerator()
        {
            if (_parameters == null || _length <= 0)
            {
                return Array.Empty<object>().GetEnumerator();
            }

            return (IEnumerator)new ArraySegment<object>(_parameters, 0, _length).GetEnumerator();
        }
    }
}
