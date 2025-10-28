using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base implementation of a hierarchical object.
//
// The position and velocity of a hierarchical object is defined as the mean of the positions and
// velocities of the sub-hierarchical objects.
public class HierarchicalBase : IHierarchical {
  // List of hierarchical objects in the hierarchy level below.
  protected List<IHierarchical> _subHierarchicals = new List<IHierarchical>();

  // Target of the hierarchical object.
  private IHierarchical _target;

  // List of hierarchical objects pursuing this hierarchical object.
  private List<IHierarchical> _pursuers = new List<IHierarchical>();

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

  public bool IsEscapingPursuers() {
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
}
