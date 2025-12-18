using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;

public class SanityTests {
  [Test]
  public void Pass() {
    Assert.Pass("This test passes.");
  }

  [UnityTest]
  public IEnumerator SanityTestWithEnumeratorPasses() {
    // Use yield to skip a frame.
    yield return null;
    Assert.Pass("This test passes after skipping a frame.");
  }
}
