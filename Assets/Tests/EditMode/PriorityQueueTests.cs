using NUnit.Framework;
using System;
using System.Collections.Generic;

public class PriorityQueueTests {
  [Test]
  public void IsEmpty_ReturnsCorrectly() {
    var queue = new PriorityQueue<float>();
    Assert.True(queue.IsEmpty());
    queue.Enqueue(item: 1f, priority: 1f);
    Assert.False(queue.IsEmpty());
  }

  [Test]
  public void Dequeue_WhenEmpty_ThrowsException() {
    var queue = new PriorityQueue<float>();
    Assert.Throws<InvalidOperationException>(() => { queue.Dequeue(); });
  }

  [Test]
  public void Peek_WhenEmpty_ThrowsException() {
    var queue = new PriorityQueue<float>();
    Assert.Throws<InvalidOperationException>(() => { queue.Peek(); });
  }

  [Test]
  public void Peek_ReturnsItemWithLowestPriority() {
    var queue = new PriorityQueue<string>();
    queue.Enqueue(item: "a", priority: 3f);
    queue.Enqueue(item: "b", priority: 1f);
    queue.Enqueue(item: "c", priority: 5f);
    queue.Enqueue(item: "d", priority: 3.2f);
    Assert.AreEqual("b", queue.Peek());
  }

  [Test]
  public void Dequeue_ReturnsItemsInIncreasingPriority() {
    var queue = new PriorityQueue<string>();
    queue.Enqueue(item: "a", priority: 3f);
    queue.Enqueue(item: "b", priority: 1f);
    queue.Enqueue(item: "c", priority: 5f);
    queue.Enqueue(item: "d", priority: 3.2f);
    var expectedOrder = new List<string> { "b", "a", "d", "c" };
    int index = 0;
    while (!queue.IsEmpty()) {
      Assert.AreEqual(expectedOrder[index], queue.Dequeue());
      ++index;
    }
  }

  [Test]
  public void Enumerator_ReturnsItemsInIncreasingPriority() {
    var queue = new PriorityQueue<string>();
    queue.Enqueue(item: "a", priority: 3f);
    queue.Enqueue(item: "b", priority: 1f);
    queue.Enqueue(item: "c", priority: 5f);
    queue.Enqueue(item: "d", priority: 3.2f);
    var expectedOrder = new List<string> { "b", "a", "d", "c" };
    var enumerator = queue.GetEnumerator();
    int index = 0;
    while (enumerator.MoveNext()) {
      Assert.AreEqual(expectedOrder[index], enumerator.Current);
      ++index;
    }
  }

  [Test]
  public void Iterator_ReturnsItemsInIncreasingPriority() {
    var queue = new PriorityQueue<string>();
    queue.Enqueue(item: "a", priority: 3f);
    queue.Enqueue(item: "b", priority: 1f);
    queue.Enqueue(item: "c", priority: 5f);
    queue.Enqueue(item: "d", priority: 3.2f);
    var expectedOrder = new List<string> { "b", "a", "d", "c" };
    int index = 0;
    foreach (var item in queue) {
      Assert.AreEqual(expectedOrder[index], item);
      ++index;
    }
  }
}
