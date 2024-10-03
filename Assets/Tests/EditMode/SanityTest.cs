using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class SanityTest {
  [Test]
  public void SanityTestSimplePasses() {
    // Use the Assert class to test conditions
    Assert.Pass("This test passes.");
  }

  // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
  // `yield return null;` to skip a frame.
  [UnityTest]
  public IEnumerator SanityTestWithEnumeratorPasses() {
    // Use the Assert class to test conditions.
    // Use yield to skip a frame.
    yield return null;
    Assert.Pass("This test passes after skipping a frame.");
  }
}