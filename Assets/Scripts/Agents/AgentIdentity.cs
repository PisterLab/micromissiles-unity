using System;

// Generates deterministic, human-readable identifiers for simulation agents.
public static class AgentIdentity {
  public static string AssetId(int configIndex) => $"AST-{ValidateIndex(configIndex):D3}";

  public static string LauncherId(int configIndex) => $"LCH-{ValidateIndex(configIndex):D3}";

  public static string ThreatId(int swarmConfigIndex, int agentIndex) =>
      $"THR-S{ValidateIndex(swarmConfigIndex):D3}-A{ValidateIndex(agentIndex):D4}";

  public static string ChildId(string parentId, Configs.AgentType agentType, int childIndex) {
    if (string.IsNullOrWhiteSpace(parentId)) {
      throw new ArgumentException("A child agent requires a nonempty parent ID.", nameof(parentId));
    }

    string childType = agentType switch {
      Configs.AgentType.CarrierInterceptor => "C",
      Configs.AgentType.MissileInterceptor => "M",
      _ => "I",
    };
    return $"{parentId}-{childType}{ValidateIndex(childIndex):D3}";
  }

  public static string FallbackInterceptorId(int spawnIndex) =>
      $"INT-{ValidateIndex(spawnIndex):D4}";

  // Returns the parent encoded in a deterministic child ID. Top-level interceptor IDs and threat
  // IDs do not have encoded parents.
  public static bool TryGetParentId(string agentId, out string parentId) {
    parentId = "";
    if (string.IsNullOrWhiteSpace(agentId)) {
      return false;
    }

    int separatorIndex = agentId.LastIndexOf('-');
    if (separatorIndex <= 0 || separatorIndex >= agentId.Length - 2) {
      return false;
    }
    char childType = agentId[separatorIndex + 1];
    if (childType != 'C' && childType != 'M' && childType != 'I') {
      return false;
    }
    for (int i = separatorIndex + 2; i < agentId.Length; ++i) {
      if (!char.IsDigit(agentId[i])) {
        return false;
      }
    }

    parentId = agentId.Substring(0, separatorIndex);
    return true;
  }

  // Compares the numeric portions of IDs as numbers, so navigation remains correctly ordered even
  // if an index grows beyond the display padding.
  public static int CompareIds(string left, string right) {
    if (ReferenceEquals(left, right)) {
      return 0;
    }
    if (left == null) {
      return -1;
    }
    if (right == null) {
      return 1;
    }

    int leftIndex = 0;
    int rightIndex = 0;
    while (leftIndex < left.Length && rightIndex < right.Length) {
      bool leftIsDigit = char.IsDigit(left[leftIndex]);
      bool rightIsDigit = char.IsDigit(right[rightIndex]);
      if (!leftIsDigit || !rightIsDigit) {
        int characterComparison = left[leftIndex].CompareTo(right[rightIndex]);
        if (characterComparison != 0) {
          return characterComparison;
        }
        ++leftIndex;
        ++rightIndex;
        continue;
      }

      int leftStart = leftIndex;
      int rightStart = rightIndex;
      while (leftIndex < left.Length && char.IsDigit(left[leftIndex])) {
        ++leftIndex;
      }
      while (rightIndex < right.Length && char.IsDigit(right[rightIndex])) {
        ++rightIndex;
      }

      int leftSignificantStart = leftStart;
      int rightSignificantStart = rightStart;
      while (leftSignificantStart < leftIndex - 1 && left[leftSignificantStart] == '0') {
        ++leftSignificantStart;
      }
      while (rightSignificantStart < rightIndex - 1 && right[rightSignificantStart] == '0') {
        ++rightSignificantStart;
      }
      int leftDigits = leftIndex - leftSignificantStart;
      int rightDigits = rightIndex - rightSignificantStart;
      if (leftDigits != rightDigits) {
        return leftDigits.CompareTo(rightDigits);
      }
      for (int i = 0; i < leftDigits; ++i) {
        int digitComparison =
            left[leftSignificantStart + i].CompareTo(right[rightSignificantStart + i]);
        if (digitComparison != 0) {
          return digitComparison;
        }
      }
    }

    int lengthComparison = left.Length.CompareTo(right.Length);
    return lengthComparison != 0 ? lengthComparison : string.CompareOrdinal(left, right);
  }

  private static int ValidateIndex(int index) {
    if (index <= 0) {
      throw new ArgumentOutOfRangeException(nameof(index), index,
                                            "Agent ID indices must be one-based.");
    }
    return index;
  }
}
