using System.Collections.Generic;

namespace Assets.Scripts.Common
{
    public class HistoryStack<T>
    {
        private LinkedList<T> _items = new LinkedList<T>();

        public int Count => _items.Count;

        public int Capacity { get; }

        public HistoryStack(int capacity)
        {
            Capacity = capacity;
        }

        public void Push(T item)
        {
            if (_items.Count == Capacity)
            {
                _items.RemoveFirst();
                _items.AddLast(item);
            }
            else
            {
                _items.AddLast(new LinkedListNode<T>(item));
            }
        }

        public T Pop()
        {
            if (_items.Count == 0)
            {
                return default;
            }

            var ls = _items.Last;
            _items.RemoveLast();
            return ls == null ? default : ls.Value;
        }
    }

}
