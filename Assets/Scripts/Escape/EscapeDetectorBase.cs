// Base implementation for an escape detector.
public abstract class EscapeDetectorBase : IEscapeDetector {
  // Agent that is pursuing the target.
  public IAgent Agent { get; init; }

  public EscapeDetectorBase(IAgent agent) {
    Agent = agent;
  }

  // Determine whether the target is escaping the agent.
  public abstract bool IsEscaping(IHierarchical target);
}
