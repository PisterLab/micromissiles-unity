using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Hierarchy node owned by an agent.
//
// Since agents cannot inherit from both HierarchicalBase and MonoBehaviour, agents use composition
// to know their position within the hierarchical strategy.
[Serializable]
public class HierarchicalAgent : HierarchicalBase {
  private static readonly IReadOnlyList<string> _emptyTargetIds = Array.Empty<string>();
  private readonly List<IAgent> _trackedTargetAgents = new List<IAgent>();

  // Raised whenever this agent's authoritative target assignment changes.
  public event Action<IAgent, IReadOnlyList<string>, IReadOnlyList<string>> OnTargetChanged;

  // Agent to which this hierarchical node belongs.
  public IAgent Agent { get; init; }

  public string TargetId => TargetIds.Count == 1 ? TargetIds[0] : "";
  public IReadOnlyList<string> TargetIds { get; private set; } = _emptyTargetIds;

  public override Vector3 Position => Agent.Position;
  public override Vector3 Velocity => Agent.Velocity;
  public override Vector3 Acceleration => Agent.Acceleration;
  public override bool IsTerminated => Agent.IsTerminated;

  public override IHierarchical Target {
    get { return base.Target; }
    set {
      IReadOnlyList<string> previousTargetIds = TargetIds;
      ClearTrackedTargetAgents();
      if (base.Target != null) {
        if (Agent.IsPursuer) {
          // Remove the interceptor as a pursuer from all target sub-hierarchical objects.
          void RemovePursuerFromHierarchical(IHierarchical target) {
            target.RemovePursuer(this);
            foreach (var subHierarchical in target.SubHierarchicals) {
              RemovePursuerFromHierarchical(subHierarchical);
            }
          }
          RemovePursuerFromHierarchical(base.Target);
        } else {
          // For non-pursuable agents, such as launchers, only remove from the top-level target.
          base.Target.RemovePursuer(this);
        }
        ClearSubHierarchicals();
        Agent.DestroyTargetModel();
      }
      base.Target = value;
      if (base.Target != null) {
        // Threats also set their target to the asset, so we should also track the threats pursuing
        // the asset.
        base.Target.AddPursuer(this);
        if (Agent is IInterceptor interceptor) {
          // Subscribe to the target events.
          foreach (var targetHierarchical in Target.ActiveSubHierarchicals) {
            if (targetHierarchical is HierarchicalAgent targetAgent) {
              targetAgent.Agent.OnTerminated += (IAgent agent) =>
                  agent.HierarchicalAgent.RemoveTargetHierarchical(targetHierarchical);
            }
          }

          // Perform recursive clustering on the new targets.
          RecursiveCluster(maxClusterSize: interceptor.CapacityPerSubInterceptor);

          if (Agent.IsPursuer) {
            // Add the interceptor as a pursuer to all target sub-hierarchical objects.
            void AddPursuerToHierarchical(IHierarchical target) {
              target.AddPursuer(this);
              foreach (var subHierarchical in target.SubHierarchicals) {
                AddPursuerToHierarchical(subHierarchical);
              }
            }
            AddPursuerToHierarchical(base.Target);
          }
        }
        Agent.CreateTargetModel(base.Target);
      }

      TrackTargetAgents(base.Target);
      TargetIds = ResolveTargetIds(base.Target);
      if (!previousTargetIds.SequenceEqual(TargetIds)) {
        OnTargetChanged?.Invoke(Agent, previousTargetIds, TargetIds);
      }
    }
  }

  public HierarchicalAgent(IAgent agent) {
    Agent = agent;
  }

  private static IReadOnlyList<string> ResolveTargetIds(IHierarchical target) {
    if (target == null) {
      return _emptyTargetIds;
    }

    List<string> targetIds = target.LeafHierarchicals(activeOnly: true, withTargetOnly: false)
                                 .OfType<HierarchicalAgent>()
                                 .Select(targetAgent => targetAgent.Agent?.AgentId)
                                 .Where(targetId => !string.IsNullOrWhiteSpace(targetId))
                                 .Distinct()
                                 .OrderBy(targetId => targetId, StringComparer.Ordinal)
                                 .ToList();
    return targetIds.Count == 0 ? _emptyTargetIds : targetIds.AsReadOnly();
  }

  private void TrackTargetAgents(IHierarchical target) {
    if (target == null) {
      return;
    }

    foreach (HierarchicalAgent targetAgent in target
                 .LeafHierarchicals(activeOnly: true, withTargetOnly: false)
                 .OfType<HierarchicalAgent>()) {
      if (targetAgent.Agent == null || _trackedTargetAgents.Contains(targetAgent.Agent)) {
        continue;
      }
      targetAgent.Agent.OnTerminated += RegisterTargetAgentTerminated;
      _trackedTargetAgents.Add(targetAgent.Agent);
    }
  }

  private void ClearTrackedTargetAgents() {
    foreach (IAgent targetAgent in _trackedTargetAgents) {
      targetAgent.OnTerminated -= RegisterTargetAgentTerminated;
    }
    _trackedTargetAgents.Clear();
  }

  private void RegisterTargetAgentTerminated(IAgent targetAgent) {
    targetAgent.OnTerminated -= RegisterTargetAgentTerminated;
    _trackedTargetAgents.Remove(targetAgent);
    IReadOnlyList<string> previousTargetIds = TargetIds;
    TargetIds = ResolveTargetIds(base.Target);
    if (!previousTargetIds.SequenceEqual(TargetIds)) {
      OnTargetChanged?.Invoke(Agent, previousTargetIds, TargetIds);
    }
  }
}
