using System;
using System.Collections.Generic;

namespace Hypertext
{
    public class ObjectPool<T> where T : new()
    {
        readonly Stack<T> stack = new Stack<T>();
        readonly Action<T> onGet;
        readonly Action<T> onRelease;

        public int CountAll { get; set; }
        public int CountActive { get { return CountAll - CountInactive; } }
        public int CountInactive { get { return stack.Count; } }

        public ObjectPool(Action<T> onGet, Action<T> onRelease)
        {
            this.onGet = onGet;
            this.onRelease = onRelease;
        }

        public T Get()
        {
            T element;
            if (stack.Count == 0)
            {
                element = new T();
                CountAll++;
            }
            else
            {
                element = stack.Pop();
            }

            if (onGet != null)
            {
                onGet(element);
            }

            return element;
        }

        public void Release(T element)
        {
            if (stack.Count > 0 && ReferenceEquals(stack.Peek(), element))
            {
                UnityEngine.Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            }

            if (onRelease != null)
            {
                onRelease(element);
            }

            stack.Push(element);
        }
    }
}
