using System.Collections;
using System.Collections.Generic;
using System.Linq;

// The priority queue dequeues elements in order of increasing priority.
public class PriorityQueue<T> : IEnumerable<T> {
  // Buffer containing the data.
  private SortedDictionary<float, Queue<T>> _buffer = new SortedDictionary<float, Queue<T>>();

  // Return whether the priority queue is empty.
  public bool IsEmpty() {
    return _buffer.Count == 0;
  }

  // Enqueue an item.
  public void Enqueue(T item, float priority) {
    if (!_buffer.ContainsKey(priority)) {
      _buffer[priority] = new Queue<T>();
    }
    _buffer[priority].Enqueue(item);
  }

  // Dequeue the item with the lowest priority value.
  public T Dequeue() {
    if (IsEmpty()) {
      throw new System.InvalidOperationException("The priority queue is empty.");
    }

    float minKey = _buffer.Keys.Min();
    Queue<T> queue = _buffer[minKey];
    T item = queue.Dequeue();
    if (queue.Count == 0) {
      _buffer.Remove(minKey);
    }
    return item;
  }

  // Peek the item with the lowest priority value.
  public T Peek() {
    if (IsEmpty()) {
      throw new System.InvalidOperationException("The priority queue is empty.");
    }

    float minKey = _buffer.Keys.Min();
    return _buffer[minKey].Peek();
  }

  // Return an enumerator for the priority queue.
  public IEnumerator<T> GetEnumerator() {
    foreach (var pair in _buffer) {
      foreach (var item in pair.Value) {
        yield return item;
      }
    }
  }
  IEnumerator IEnumerable.GetEnumerator() {
    return GetEnumerator();
  }
}
