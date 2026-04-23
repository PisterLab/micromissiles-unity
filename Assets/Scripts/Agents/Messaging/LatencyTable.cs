using System;
using System.Collections.Generic;
using UnityEngine;

// Latency Table keeps track of the delay between every agent.
// Table is n * n sized, n is the size of CommsNode.

public enum CommsNode {
  IADS = 0,
  Carrier = 1,
  Interceptor = 2,
}

// Setting everything to 0 should behave like with no latency.
public sealed class LatencyTable {
  private static readonly CommsNode[] Nodes = (CommsNode[])Enum.GetValues(typeof(CommsNode));
  private static readonly Dictionary<CommsNode, int> NodeIndices = CreateNodeIndices();
  private readonly float[,] _latency;

  // Creates empty _latency array with defaultSeconds seconds each.
  public LatencyTable(float defaultSeconds = 0f) {
    int n = Nodes.Length;
    _latency = new float[n, n];
    if (defaultSeconds == 0f) {
      return;
    }
    for (int i = 0; i < n; i++) {
      for (int j = 0; j < n; j++) {
        _latency[i, j] = Mathf.Max(0f, defaultSeconds);
      }
    }
  }

  public void Set(CommsNode from, CommsNode to, float seconds) {
    _latency[GetIndex(from), GetIndex(to)] = Mathf.Max(0f, seconds);
  }

  public float Get(CommsNode from, CommsNode to) {
    return _latency[GetIndex(from), GetIndex(to)];
  }

  // Used for quick GetIndex lookup and make sure enum always starts with 0 to N-1.
  private static Dictionary<CommsNode, int> CreateNodeIndices() {
    var indices = new Dictionary<CommsNode, int>(Nodes.Length);
    for (int i = 0; i < Nodes.Length; ++i) {
      indices[Nodes[i]] = i;
    }
    return indices;
  }

  private static int GetIndex(CommsNode node) {
    if (!NodeIndices.TryGetValue(node, out int index)) {
      throw new ArgumentOutOfRangeException(nameof(node), node, "Unknown Interceptor Type.");
    }
    return index;
  }
}
