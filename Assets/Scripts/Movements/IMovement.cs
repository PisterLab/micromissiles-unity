// Interface for a movement behavior.
//
// The movement behavior determines how the agent navigates through the environment, such as whether
// it is aerial or ground-based. The movement behavior also considers drag and gravity.
public interface IMovement {
  IAgent Agent { get; set; }

  // Determine the next movement for the agent by using the agent's controller to calculate the
  // acceleration input.
  void Update(double deltaTime);
}
