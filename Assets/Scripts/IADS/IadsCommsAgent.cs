using UnityEngine;

// IADS-owned mailbox endpoint. This exists so communication can use a dedicated comms node rather
// than forcing the IADS through the physical-agent interface.

public class IadsCommsAgent : MonoBehaviour, ICommsNodeOwner {
  private CommsNode _commsNode;

  public CommsNode CommsNode => _commsNode ??= new CommsNode(this);

  private void OnDestroy() {
    _commsNode?.Terminate();
  }
}
