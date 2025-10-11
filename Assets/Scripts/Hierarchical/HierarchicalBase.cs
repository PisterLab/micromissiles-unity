using System.Collections.Generic;
using UnityEngine;

// Base implementation of a hierarchical object.
//
// The position and velocity of a hierarchical object is defined as the mean of the positions and
// velocities of the sub-hierarchical objects.
public class HierarchicalBase : IHierarchical {
  // List of hierarchical objects in the hierarchy level below.
  [SerializeField]
  private List<HierarchicalBase> _subHierarchicals = new List<HierarchicalBase>();

  // Target of the hierarchical object.
  [SerializeField]
  private HierarchicalBase _target;

  // Target model of the hierarchical object. The target model is updated by the sensor and should
  // be used by the controller to model imperfect knowledge of the engagement.
  [SerializeField]
  private HierarchicalBase _targetModel;

  public IReadOnlyList<IHierarchical> SubHierarchicals => _subHierarchicals.AsReadOnly();
  public IHierarchical Target {
    get => _target;
    set => _target = value as HierarchicalBase;
  }
  public IHierarchical TargetModel {
    get => _targetModel;
    set => _targetModel = value as HierarchicalBase;
  }
  public Vector3 Position => GetPosition();
  public Vector3 Velocity => GetVelocity();
  public float Speed => Velocity.magnitude;

  public void AddSubHierarchical(IHierarchical subHierarchical) {
    if (!_subHierarchicals.Contains(subHierarchical as HierarchicalBase)) {
      _subHierarchicals.Add(subHierarchical as HierarchicalBase);
    }
  }

  public void RemoveSubHierarchical(IHierarchical subHierarchical) {
    if (_subHierarchicals.Contains(subHierarchical as HierarchicalBase)) {
      _subHierarchicals.Remove(subHierarchical as HierarchicalBase);
    }
  }

  protected virtual Vector3 GetPosition() {
    if (_subHierarchicals.Count == 0) {
      return Vector3.zero;
    }
    // Return the mean of the positions of the sub-hierarchical objects.
    Vector3 sum = Vector3.zero;
    foreach (var subHierarchical in _subHierarchicals) {
      sum += subHierarchical.GetPosition();
    }
    return sum / _subHierarchicals.Count;
  }

  protected virtual Vector3 GetVelocity() {
    if (_subHierarchicals.Count == 0) {
      return Vector3.zero;
    }
    // Return the mean of the velocities of the sub-hierarchical objects.
    Vector3 sum = Vector3.zero;
    foreach (var subHierarchical in _subHierarchicals) {
      sum += subHierarchical.GetVelocity();
    }
    return sum / _subHierarchicals.Count;
  }
}
