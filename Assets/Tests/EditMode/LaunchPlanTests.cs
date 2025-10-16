using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

public class LaunchPlanTests {
  private const float _epsilon = 1e-3f;

  [Test]
  public void NoLaunch_ShouldHaveShouldLaunchFalse() {
    var plan = LaunchPlan.NoLaunch();
    Assert.IsFalse(plan.ShouldLaunch);
  }

  [Test]
  public void NormalizedLaunchVector_IsNormalized() {
    var launchPlan = new LaunchPlan {
      ShouldLaunch = true,
      LaunchAngle = 45f,
      RelativeInterceptPosition = new Vector3(1, 1, 0),
    };
    var launchVector = launchPlan.NormalizedLaunchVector();
    Assert.AreEqual(1f, launchVector.magnitude, _epsilon);
  }

  [Test]
  public void NormalizedLaunchVector_PointsVertically() {
    var launchPlan = new LaunchPlan {
      ShouldLaunch = true,
      LaunchAngle = 90f,
      RelativeInterceptPosition = new Vector3(1, 1, 0),
    };
    var launchVector = launchPlan.NormalizedLaunchVector();
    Assert.That(launchVector,
                Is.EqualTo(new Vector3(0, 1f, 0)).Using(Vector3EqualityComparer.Instance));
  }

  [Test]
  public void NormalizedLaunchVector_PointsHorizontally() {
    var launchPlan = new LaunchPlan {
      ShouldLaunch = true,
      LaunchAngle = 0f,
      RelativeInterceptPosition = new Vector3(1, 1, 0),
    };
    var launchVector = launchPlan.NormalizedLaunchVector();
    Assert.That(launchVector,
                Is.EqualTo(new Vector3(1f, 0, 0)).Using(Vector3EqualityComparer.Instance));
  }

  [Test]
  public void NormalizedLaunchVector_PointsDiagonally() {
    var launchPlan = new LaunchPlan {
      ShouldLaunch = true,
      LaunchAngle = 45f,
      RelativeInterceptPosition = new Vector3(1, 0, 1),
    };
    var launchVector = launchPlan.NormalizedLaunchVector();
    Assert.That(launchVector, Is.EqualTo(new Vector3(1, Mathf.Sqrt(2f), 1).normalized)
                                  .Using(Vector3EqualityComparer.Instance));
  }
}
