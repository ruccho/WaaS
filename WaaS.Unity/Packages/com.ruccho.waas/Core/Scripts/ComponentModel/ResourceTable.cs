using System;

namespace WaaS.ComponentModel
{
    public class ResourceTable<T>
    {
        private readonly object locker = new();
        private Element[] elements;
        private int head;

        public ResourceTable(int capacity = 32)
        {
            elements = new Element[capacity];
            head = 0;
        }

        public int Add(T value)
        {
            lock (locker)
            {
                ref var headElement = ref elements[head];

                headElement.resource = value;
                headElement.isOccupied = true;

                var next = headElement.Next;
                if (next == -1)
                {
                    next = head + 1;
                    if (next >= elements.Length)
                    {
                        // expand
                        var newElements = new Element[elements.Length * 2];
                        elements.CopyTo(newElements, 0);
                        elements = newElements;
                    }
                }

                var result = head;

                head = next;
                return result;
            }
        }

        public T RemoveAt(int index)
        {
            lock (locker)
            {
                ref var element = ref elements[index];

                if (!element.isOccupied) throw new InvalidOperationException("Element is already free");

                var result = element.resource;

                element.Next = head;
                element.resource = default;
                element.isOccupied = false;
                head = index;

                return result;
            }
        }

        public T Get(int index)
        {
            lock (locker)
            {
                ref var element = ref elements[index];
                if (element.isOccupied) throw new InvalidOperationException("Element is already free");

                return element.resource;
            }
        }

        private struct Element
        {
            public T resource;
            private int nextPlusOne;
            public bool isOccupied;

            public int Next
            {
                get => nextPlusOne - 1;
                set => nextPlusOne = value - 1;
            }
        }
    }
}