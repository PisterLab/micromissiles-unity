using System.Collections.Generic;
using UnityEngine;

// Base implementation of a hierarchical object.
//
// The position and velocity of a hierarchical object is defined as the mean of the positions and
// velocities of the sub-hierarchical objects.
public class HierarchicalBase : IHierarchical {
  // List of hierarchical objects in the hierarchy level below.
  private List<IHierarchical> _subHierarchicals = new List<IHierarchical>();

  // Target of the hierarchical object.
  private IHierarchical _target;

  // Target model of the hierarchical object. The target model is updated by the sensor and should
  // be used by the controller to model imperfect knowledge of the engagement.
  private IHierarchical _targetModel;

  public IReadOnlyList<IHierarchical> SubHierarchicals => _subHierarchicals.AsReadOnly();
  public IHierarchical Target {
    get => _target;
    set => _target = value;
  }
  public IHierarchical TargetModel {
    get => _targetModel;
    set => _targetModel = value;
  }
  public Vector3 Position => GetPosition();
  public Vector3 Velocity => GetVelocity();
  public float Speed => Velocity.magnitude;

  public void AddSubHierarchical(IHierarchical subHierarchical) {
    if (!_subHierarchicals.Contains(subHierarchical)) {
      _subHierarchicals.Add(subHierarchical);
    }
  }

  public void RemoveSubHierarchical(IHierarchical subHierarchical) {
    _subHierarchicals.Remove(subHierarchical);
  }

  protected virtual Vector3 GetPosition() {
    return GetMean(s => s.Position);
  }

  protected virtual Vector3 GetVelocity() {
    return GetMean(s => s.Velocity);
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
