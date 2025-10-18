// Base implementation of a movement behavior.
public abstract class MovementBase : IMovement {
  // Agent to which the movement behavior is assigned.
  private IAgent _agent;

  public IAgent Agent {
    get => _agent;
    set => _agent = value;
  }

  public abstract void Update(double deltaTime);
}
