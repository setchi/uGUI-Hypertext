/*
 * uGUI-Hypertext (https://github.com/setchi/uGUI-Hypertext)
 * Copyright (c) 2019 setchi
 * Licensed under MIT (https://github.com/setchi/uGUI-Hypertext/blob/master/LICENSE)
 */

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
        public int CountActive => CountAll - CountInactive;
        public int CountInactive => stack.Count;

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

            onGet?.Invoke(element);

            return element;
        }

        public void Release(T element)
        {
            if (stack.Count > 0 && ReferenceEquals(stack.Peek(), element))
            {
                UnityEngine.Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            }

            onRelease?.Invoke(element);

            stack.Push(element);
        }
    }
}
