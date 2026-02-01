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
                if (_parameters == null)
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
            return _parameters?.GetEnumerator() ?? Array.Empty<object>().GetEnumerator();
        }
    }
}
