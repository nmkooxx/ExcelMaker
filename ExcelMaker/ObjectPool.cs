using System.Collections.Generic;

namespace CsvHelper {
    public class ObjectPool<T> where T : new() {
        private Queue<T> m_Queue = new Queue<T>(10);

        public T Pop() {
            if (m_Queue.Count > 0) {
                return m_Queue.Dequeue();
            }
            return new T();
        }

        public void Push(T t) {
            m_Queue.Enqueue(t);
        }

        public void Clear() {
            m_Queue.Clear();
        }

    }
}