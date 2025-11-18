// The planner release strategy uses a launch planner to determine when to launch an interceptor
// against an incoming target.
public class PlannerReleaseStrategy : SingleReleaseStrategyBase {
  public ILaunchPlanner Planner { get; set; }

  public PlannerReleaseStrategy(IAgent agent, ILaunchPlanner planner) : base(agent) {
    Planner = planner;
  }

  protected override LaunchPlan PlanRelease(IHierarchical target) {
    Planner.Predictor.Hierarchical = target;
    return Planner.Plan();
  }
}
