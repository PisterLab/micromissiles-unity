// Base implementation of a movement behavior.
public abstract class MovementBase : IMovement {
  // Agent to which the movement behavior is assigned.
  private IAgent _agent;

  public IAgent Agent {
    get => _agent;
    set => _agent = value;
  }

  public MovementBase(IAgent agent) {
    _agent = agent;
  }

  // Determine the next movement for the agent by using the agent's controller to calculate the
  // acceleration input.
  public abstract void Update(double deltaTime);
}
