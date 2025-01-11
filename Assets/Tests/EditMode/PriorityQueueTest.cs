using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

public class PriorityQueueTest {
  [Test]
  public void TestIsEmpty() {
    PriorityQueue<float> queue = new PriorityQueue<float>();
    Assert.True(queue.IsEmpty());
    queue.Enqueue(item: 1.0f, priority: 1.0f);
    Assert.False(queue.IsEmpty());
  }

  [Test]
  public void TestDequeueWhenEmpty() {
    PriorityQueue<float> queue = new PriorityQueue<float>();
    Assert.Throws<InvalidOperationException>(() => { queue.Dequeue(); });
  }

  [Test]
  public void TestPeekWhenEmpty() {
    PriorityQueue<float> queue = new PriorityQueue<float>();
    Assert.Throws<InvalidOperationException>(() => { queue.Peek(); });
  }

  [Test]
  public void TestPeek() {
    PriorityQueue<string> queue = new PriorityQueue<string>();
    queue.Enqueue(item: "a", priority: 3.0f);
    queue.Enqueue(item: "b", priority: 1.0f);
    queue.Enqueue(item: "c", priority: 5.0f);
    queue.Enqueue(item: "d", priority: 3.2f);
    Assert.AreEqual("b", queue.Peek());
  }

  [Test]
  public void TestPriority() {
    PriorityQueue<string> queue = new PriorityQueue<string>();
    queue.Enqueue(item: "a", priority: 3.0f);
    queue.Enqueue(item: "b", priority: 1.0f);
    queue.Enqueue(item: "c", priority: 5.0f);
    queue.Enqueue(item: "d", priority: 3.2f);

    List<string> expectedOrder = new List<string> { "b", "a", "d", "c" };
    int index = 0;
    while (!queue.IsEmpty()) {
      Assert.AreEqual(expectedOrder[index], queue.Dequeue());
      ++index;
    }
  }

  [Test]
  public void TestEnumerator() {
    PriorityQueue<string> queue = new PriorityQueue<string>();
    queue.Enqueue(item: "a", priority: 3.0f);
    queue.Enqueue(item: "b", priority: 1.0f);
    queue.Enqueue(item: "c", priority: 5.0f);
    queue.Enqueue(item: "d", priority: 3.2f);

    List<string> expectedOrder = new List<string> { "b", "a", "d", "c" };
    IEnumerator<string> enumerator = queue.GetEnumerator();
    int index = 0;
    while (enumerator.MoveNext()) {
      Assert.AreEqual(expectedOrder[index], enumerator.Current);
      ++index;
    }
  }

  [Test]
  public void TestIterator() {
    PriorityQueue<string> queue = new PriorityQueue<string>();
    queue.Enqueue(item: "a", priority: 3.0f);
    queue.Enqueue(item: "b", priority: 1.0f);
    queue.Enqueue(item: "c", priority: 5.0f);
    queue.Enqueue(item: "d", priority: 3.2f);

    List<string> expectedOrder = new List<string> { "b", "a", "d", "c" };
    int index = 0;
    foreach (var item in queue) {
      Assert.AreEqual(expectedOrder[index], item);
      ++index;
    }
  }
}
