using System;
using UnityEngine;


// Latency Table keeps track of the delay between every agent.
// Table is n * n sized, n is the size of CommsNode.

public enum CommsNode {
    IADS = 0,
    Carrier = 1,
    Interceptor = 2,
}

// Setting everything to 0 should behave like with no latency
public sealed class LatencyTable {
    private readonly float[,] _latency;

    // creates empty _latency array with defaultSeconds seconds each
    public LatencyTable(float defaultSeconds = 0f) {
        int n = Enum.GetValues(typeof(CommsNode)).Length;
        _latency = new float[n, n];
        for (int i = 0; i < n; i++) {
            for (int j = 0; j < n; j++) {
                _latency[i, j] = defaultSeconds;
            }
        }
    }

    public void Set(CommsNode from, CommsNode to, float seconds) {
        _latency[(int) from, (int) to] = Mathf.Max(0f, seconds);
    }

    public float Get(CommsNode from, CommsNode to) {
        return _latency[(int) from, (int) to];
    }
}
