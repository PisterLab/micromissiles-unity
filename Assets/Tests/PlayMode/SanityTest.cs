using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SanityTest {
  [UnityTest]
  public IEnumerator SanityCheck() {
    // Arrange
    GameObject testObject = new GameObject("TestObject");

    // Act
    testObject.AddComponent<BoxCollider>();

    // Assert
    Assert.IsTrue(testObject.GetComponent<BoxCollider>() != null,
                  "BoxCollider should be added to the test object");

    // Clean up
    Object.Destroy(testObject);

    yield return null;
  }
}