using System.Collections.Generic;
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

  public virtual IHierarchical Target {
    get { return _target; }
    set {
      if (_target != null) {
        _target.RemovePursuer(this);
      }
      _target = value;
      _target.AddPursuer(this);
    }
  }

  public IReadOnlyList<IHierarchical> Pursuers => _pursuers.AsReadOnly();

  public Vector3 Position => GetPosition();
  public Vector3 Velocity => GetVelocity();
  public float Speed => Velocity.magnitude;
  public Vector3 Acceleration => GetAcceleration();

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

  protected virtual Vector3 GetPosition() {
    return GetMean(s => s.Position);
  }

  protected virtual Vector3 GetVelocity() {
    return GetMean(s => s.Velocity);
  }

  protected virtual Vector3 GetAcceleration() {
    return GetMean(s => s.Acceleration);
  }

  private Vector3 GetMean(System.Func<IHierarchical, Vector3> selector) {
    if (_subHierarchicals.Count == 0) {
      return Vector3.zero;
    }

    Vector3 sum = Vector3.zero;
    foreach (var subHierarchical in _subHierarchicals) {
      sum += selector(subHierarchical);
    }
    return sum / _subHierarchicals.Count;
  }
}
