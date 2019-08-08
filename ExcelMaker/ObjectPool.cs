using System.Collections.Generic;

namespace CsvHelper {
    public class ObjectPool<T> where T : new() {
        private Queue<T> m_queue = new Queue<T>(10);

        public T Pop() {
            if (m_queue.Count > 0) {
                return m_queue.Dequeue();
            }
            return new T();
        }

        public void Push(T t) {
            m_queue.Enqueue(t);
        }

        public void Clear() {
            m_queue.Clear();
        }

    }
}