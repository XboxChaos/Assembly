using System.Collections.Generic;
using System.Linq;

namespace Blamite.Util
{
    // https://stackoverflow.com/a/13319279
    public class FIFOStack<T> : LinkedList<T>
    {
        public T Pop()
        {
            T first = this.First();
            RemoveFirst();
            return first;
        }

        public void Push(T item)
        {
            AddLast(item);
        }
    }
}
