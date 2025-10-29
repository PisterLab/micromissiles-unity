using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of a hierarchical object.
//
// The position and velocity of a hierarchical object is defined as the mean of the positions and
// velocities of the sub-hierarchical objects.
public class HierarchicalBase : IHierarchical {
  public event HierarchicalEventHandler OnHit;
  public event HierarchicalEventHandler OnMiss;

  // List of hierarchical objects in the hierarchy level below.
  protected List<IHierarchical> _subHierarchicals = new List<IHierarchical>();

  // Target of the hierarchical object.
  private IHierarchical _target;

  // List of hierarchical objects pursuing this hierarchical object.
  private List<IHierarchical> _pursuers = new List<IHierarchical>();

  // List of targets

  public IReadOnlyList<IHierarchical> SubHierarchicals => _subHierarchicals.AsReadOnly();

  // Return a list of active sub-hierarchical objects.
  public IEnumerable<IHierarchical> ActiveSubHierarchicals =>
      _subHierarchicals.Where(s => !s.IsTerminated);

  public virtual IHierarchical Target {
    get { return _target; }
    set {
      if (_target != null) {
        _target.RemovePursuer(this);
      }
      _target = value;
      if (_target != null) {
        _target.AddPursuer(this);
      }
    }
  }

  public IReadOnlyList<IHierarchical> Pursuers => _pursuers.AsReadOnly();

  public virtual Vector3 Position => GetMean(s => s.Position);
  public virtual Vector3 Velocity => GetMean(s => s.Velocity);
  public float Speed => Velocity.magnitude;
  public virtual Vector3 Acceleration => GetMean(s => s.Acceleration);
  public virtual bool IsTerminated => !ActiveSubHierarchicals.Any();

  public void AddSubHierarchical(IHierarchical subHierarchical) {
    if (!_subHierarchicals.Contains(subHierarchical)) {
      _subHierarchicals.Add(subHierarchical);
    }
  }

  public void RemoveSubHierarchical(IHierarchical subHierarchical) {
    _subHierarchicals.Remove(subHierarchical);
  }

  public void ClearSubHierarchicals() {
    _subHierarchicals.Clear();
  }

  public void AddPursuer(IHierarchical pursuer) {
    if (!_pursuers.Contains(pursuer)) {
      _pursuers.Add(pursuer);
    }
  }

  public void RemovePursuer(IHierarchical pursuer) {
    _pursuers.Remove(pursuer);
  }

  public void HandleHit() {
    OnHit?.Invoke(this, Target);
  }

  public void HandleMiss() {
    OnMiss?.Invoke(this, Target);
  }

  public void RegisterSubHierarchicalHit(IHierarchical subHierarchical, IHierarchical target) {
    // Re-assign the other pursuers of the target to other active targets.
    var otherPursuers = target.Pursuers.Where(pursuer => pursuer != subHierarchical).ToList();
    // Use a maximum speed assignment.
    IAssignment targetAssignment =
        new MaxSpeedAssignment(Assignment.Assignment_EvenAssignment_Assign);
    var activeTargets = Target.ActiveSubHierarchicals.ToList();
    // TODO(titan): If there are no active targets left, report to the next hierarchical object
    // above that these sub-hierarchical objects are available for re-assignment.
    List<AssignmentItem> assignments = targetAssignment.Assign(otherPursuers, activeTargets);
    foreach (var assignment in assignments) {
      assignment.First.Target = assignment.Second;
    }
  }

  public void RegisterSubHierarchicalMiss(IHierarchical subHierarchical, IHierarchical target) {
    // If a sub-hierarchical object misses a target, the parent hierarchical object should in the
    // following order:
    //  1. Re-assign the target to another sub-hierarchical object without leaving another target
    //  uncovered.
    //  2. Launch another sub-hierarchical object to pursue the target.
    //  3. Propagate the target miss to the next hierarchical object above.
    var otherPursuers =
        Target.ActiveSubHierarchicals.SelectMany(subHierarchical => subHierarchical.Pursuers)
            .Where(pursuer => pursuer != subHierarchical)
            .ToList();
    var activeTargets = Target.ActiveSubHierarchicals.ToList();
    if (otherPursuers.Count >= activeTargets.Count) {
      // Re-assigning at least one sub-hierarchical object to pursue the missed target without
      // leaving another target uncovered is possible. Use a cover assignment.
      IAssignment targetAssignment =
          new MaxSpeedAssignment(Assignment.Assignment_CoverAssignment_Assign);
      List<AssignmentItem> assignments = targetAssignment.Assign(otherPursuers, activeTargets);
      foreach (var assignment in assignments) {
        assignment.First.Target = assignment.Second;
      }
      return;
    }

    // If re-assignment is not possible, queue up the targets to prepare another sub-hierarchical
    // object to be launched or to be clustered for the next hierarchical object above.
    return;
  }

  private Vector3 GetMean(System.Func<IHierarchical, Vector3> selector) {
    Vector3 sum = Vector3.zero;
    int count = 0;
    foreach (var subHierarchical in ActiveSubHierarchicals) {
      sum += selector(subHierarchical);
      ++count;
    }
    if (count == 0) {
      return Vector3.zero;
    }
    return sum / count;
  }

  private bool IsEscapingPursuers() {
    return Pursuers.All(pursuer => {
      // A hierarchical object is considered escaping a pursuer if the closing velocity is
      // non-positive or if the hierarchical object will reach its target before the pursuer reaches
      // the hierarchical object.
      Vector3 relativePosition = pursuer.Position - Position;
      Vector3 relativeVelocity = pursuer.Velocity - Velocity;
      float rangeRate = Vector3.Dot(relativeVelocity, relativePosition.normalized);
      float closingVelocity = -rangeRate;
      if (closingVelocity <= 0) {
        return true;
      }
      float pursuerDistance = relativePosition.magnitude;
      float targetDistance = (Target.Position - Position).magnitude;
      float timeToTarget = targetDistance / Speed;
      float pursuerTimeToIntercept = pursuerDistance / pursuer.Speed;
      return pursuerDistance > targetDistance && timeToTarget < pursuerTimeToIntercept;
    });
  }
}
