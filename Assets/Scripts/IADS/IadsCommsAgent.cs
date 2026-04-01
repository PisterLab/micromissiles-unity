using System;
using UnityEngine;

/* IADS Proxy for supporting mailbox message sending and recieving. */

public class IadsCommsAgent : MonoBehaviour, IAgent {
  public event AgentTerminatedEventHandler OnTerminated;

  [SerializeReference]
  private HierarchicalAgent _hierarchicalAgent;

  [SerializeField]
  private Vector3 _velocity = Vector3.zero;

  [SerializeField]
  private Vector3 _acceleration = Vector3.zero;

  [SerializeField]
  private Vector3 _accelerationInput = Vector3.zero;

  public HierarchicalAgent HierarchicalAgent {
    get => _hierarchicalAgent;
    set => _hierarchicalAgent = value;
  }

  public Configs.StaticConfig StaticConfig { get; set; }
  public Configs.AgentConfig AgentConfig { get; set; }
  public IMovement Movement { get; set; }
  public IController Controller { get; set; }
  public ISensor Sensor { get; set; }
  public IAgent TargetModel { get; set; }

  public Vector3 Position {
    get => transform.position;
    set => transform.position = value;
  }

  public Vector3 Velocity {
    get => _velocity;
    set => _velocity = value;
  }

  public float Speed => _velocity.magnitude;

  public Vector3 Acceleration {
    get => _acceleration;
    set => _acceleration = value;
  }

  public Vector3 AccelerationInput {
    get => _accelerationInput;
    set => _accelerationInput = value;
  }

  public bool IsPursuer => false;
  public float ElapsedTime => SimManager.Instance != null ? SimManager.Instance.ElapsedTime : Time.time;
  public bool IsTerminated { get; private set; } = false;
  public CommsNode NodeType => CommsNode.IADS;

  public Transform Transform => transform;
  public Vector3 Up => transform.up;
  public Vector3 Forward => transform.forward;
  public Vector3 Right => transform.right;
  public Quaternion InverseRotation => Quaternion.Inverse(transform.rotation);

  public float MaxForwardAcceleration() {
    throw CreateUnsupportedException(nameof(MaxForwardAcceleration));
  }

  public float MaxNormalAcceleration() {
    throw CreateUnsupportedException(nameof(MaxNormalAcceleration));
  }

  public void CreateTargetModel(IHierarchical target) {
    throw CreateUnsupportedException(nameof(CreateTargetModel));
  }

  public void DestroyTargetModel() {
    throw CreateUnsupportedException(nameof(DestroyTargetModel));
  }

  public void UpdateTargetModel() {
    throw CreateUnsupportedException(nameof(UpdateTargetModel));
  }

  public void Terminate() {
    if (IsTerminated) { return; }
    IsTerminated = true;
    OnTerminated?.Invoke(this);
  }

  public Transformation GetRelativeTransformation(IAgent target) {
    throw CreateUnsupportedException($"{nameof(GetRelativeTransformation)}(IAgent)");
  }

  public Transformation GetRelativeTransformation(IHierarchical target) {
    throw CreateUnsupportedException($"{nameof(GetRelativeTransformation)}(IHierarchical)");
  }

  public Transformation GetRelativeTransformation(in Vector3 waypoint) {
    throw CreateUnsupportedException($"{nameof(GetRelativeTransformation)}(Vector3)");
  }

  private NotSupportedException CreateUnsupportedException(string memberName) {
    string targetModelState =
        TargetModel == null ? " TargetModel is null on this proxy." :
                              " TargetModel is set on this proxy, which indicates the mailbox-only IADS proxy is being used as a physical agent.";
    return new NotSupportedException(
        $"{nameof(IadsCommsAgent)}.{memberName} is unsupported because {nameof(IadsCommsAgent)} is a comms-only mailbox proxy, not a physical/sensing agent.{targetModelState}");
  }
}
