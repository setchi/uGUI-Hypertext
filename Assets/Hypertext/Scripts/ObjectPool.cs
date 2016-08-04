using System;
using System.Collections.Generic;

namespace HypertextHelper
{
    public class ObjectPool<T> where T : new()
    {
        readonly Stack<T> _stack = new Stack<T>();
        readonly Action<T> _onGet;
        readonly Action<T> _onRelease;

        public int CountAll { get; set; }
        public int CountActive { get { return CountAll - CountInactive; } }
        public int CountInactive { get { return _stack.Count; } }

        public ObjectPool(Action<T> onGet, Action<T> onRelease)
        {
            _onGet = onGet;
            _onRelease = onRelease;
        }

        public T Get()
        {
            T element;
            if (_stack.Count == 0)
            {
                element = new T();
                CountAll++;
            }
            else
            {
                element = _stack.Pop();
            }

            if (_onGet != null)
            {
                _onGet(element);
            }

            return element;
        }

        public void Release(T element)
        {
            if (_stack.Count > 0 && ReferenceEquals(_stack.Peek(), element))
            {
                UnityEngine.Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            }

            if (_onRelease != null)
            {
                _onRelease(element);
            }

            _stack.Push(element);
        }
    }
}
