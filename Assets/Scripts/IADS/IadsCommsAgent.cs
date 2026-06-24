using UnityEngine;

// IADS-owned mailbox endpoint. This exists so communication can use a dedicated comms node rather
// than forcing the IADS through the physical-agent interface.

public class IadsCommsAgent : MonoBehaviour, ICommsNodeOwner {
  public CommsNode CommsNode { get; } = new CommsNode();

  private void OnDestroy() {
    CommsNode?.Terminate();
  }
}
