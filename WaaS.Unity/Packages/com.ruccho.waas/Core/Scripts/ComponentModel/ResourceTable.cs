using System;

namespace WaaS.ComponentModel
{
    public class ResourceTable<T>
    {
        private readonly object locker = new();
        private readonly bool reserveZero;
        private Element[] elements;
        private int head;

        public ResourceTable(int capacity = 32, bool reserveZero = true)
        {
            this.reserveZero = reserveZero;
            elements = new Element[capacity];
            head = 0;
            if (reserveZero) head = 1;
        }

        public int Add(T value)
        {
            lock (locker)
            {
                ref var headElement = ref elements[head];

                headElement.rep = value;
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
            if (reserveZero && index == 0) throw new InvalidOperationException("Cannot remove element at index 0");
            lock (locker)
            {
                ref var element = ref elements[index];

                if (!element.isOccupied) throw new InvalidOperationException("Element is already free");

                var result = element.rep;

                element.Next = head;
                element.rep = default;
                element.isOccupied = false;
                head = index;
                return result;
            }
        }

        public T Get(int index)
        {
            if (reserveZero && index == 0) throw new InvalidOperationException("Cannot get element at index 0");
            lock (locker)
            {
                ref var element = ref elements[index];
                if (!element.isOccupied) throw new InvalidOperationException("Element is already free");

                return element.rep;
            }
        }

        private struct Element
        {
            public T rep;
            private int nextPlusOne;
            public bool isOccupied;

            public int Next
            {
                get => nextPlusOne - 1;
                set => nextPlusOne = value + 1;
            }
        }
    }
}