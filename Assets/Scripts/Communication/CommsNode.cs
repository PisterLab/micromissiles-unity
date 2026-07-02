using System;

public interface ICommsEndpoint {
  CommsNode CommsNode { get; }
}

// Threat endpoint types are defined for future expansion, but threats do not currently participate
// in the communication flow. Threats can later be used on the defense side.
public enum CommsEndpointType {
  Invalid,
  Iads,
  Vessel,
  ShoreBattery,
  CarrierInterceptor,
  MissileInterceptor,
  FixedWingThreat,
  RotaryWingThreat,
}

// CommsNode is the mailbox-facing endpoint owned by an agent or system such as the IADS.
public sealed class CommsNode {
  public CommsEndpointType EndpointType { get; }

  private bool _isTerminated;

  public event Action<Message> OnMessageReceived;

  public bool IsTerminated => _isTerminated;

  public CommsNode(CommsEndpointType endpointType = CommsEndpointType.Invalid) {
    EndpointType = endpointType;
  }

  public void Receive(Message message) {
    if (message == null) {
      throw new ArgumentNullException(nameof(message));
    }
    OnMessageReceived?.Invoke(message);
  }

  public void Terminate() {
    _isTerminated = true;
  }
}
