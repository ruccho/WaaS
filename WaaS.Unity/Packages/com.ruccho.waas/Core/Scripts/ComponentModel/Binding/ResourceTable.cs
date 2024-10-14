using System;

namespace WaaS.ComponentModel.Binding
{
    public class ResourceTable<T>
    {
        private Element[] elements;
        private int head;

        public ResourceTable(int capacity = 32)
        {
            elements = new Element[capacity];
            head = 0;
        }

        public int Add(T value)
        {
            ref var headElement = ref elements[head];

            headElement.resource = value;
            headElement.isOccupied = true;

            var next = headElement.Next;
            if (next == -1)
            {
                next = head + 1;
                if (head + 1 >= elements.Length)
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

        public void RemoveAt(int index)
        {
            ref var element = ref elements[index];

            if (!element.isOccupied) throw new InvalidOperationException("Element is already free");

            element.Next = head;
            element.resource = default;
            element.isOccupied = false;
            head = index;
        }

        public T Get(int index)
        {
            ref var element = ref elements[head];
            if (element.isOccupied) throw new InvalidOperationException("Element is already free");

            return element.resource;
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