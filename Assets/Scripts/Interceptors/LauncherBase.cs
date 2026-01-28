using UnityEngine;

// Base implementation of a launcher.
//
// A launcher is the source of interceptors and cannot be released by other interceptors.
// Interceptors can be launched from naval vessels, shore batteries, or mobile platforms, for
// example.
public abstract class LauncherBase : CarrierBase {
  // Launchers cannot pursue threats.
  public override bool IsPursuer => false;

  // Launchers cannot be reassigned to other targets and rely on threat reassignment only.
  public override bool IsReassignable => false;

  protected override void Awake() {
    base.Awake();

    // TODO(titan): The predictor, launch angle planner, and launch planner should be defined in the
    // simulation configuration.
    var launchAnglePlanner = new LaunchAngleCsvInterpolator(this);
    var predictor = new LinearExtrapolator(hierarchical: null);
    var planner = new IterativeLaunchPlanner(launchAnglePlanner, predictor);
    ReleaseStrategy = new PlannerReleaseStrategy(this, planner);
  }
}
