using System;
using System.Runtime.InteropServices;

public class Assignment {
// Assign the agents to the tasks using a cover assignment.
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
  [DllImport("assignment")]
#else   // !UNITY_EDITOR_WIN || !UNITY_STANDALONE_WIN
  [DllImport("libassignment")]
#endif  // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
  public static extern int Assignment_CoverAssignment_Assign(int numAgents, int numTasks,
                                                             float[] costs, IntPtr assignedAgents,
                                                             IntPtr assignedTasks);

// Assign the agents to the tasks using an even assignment.
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
  [DllImport("assignment")]
#else   // !UNITY_EDITOR_WIN || !UNITY_STANDALONE_WIN
  [DllImport("libassignment")]
#endif  // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
  public static extern int Assignment_EvenAssignment_Assign(int numAgents, int numTasks,
                                                            float[] costs, IntPtr assignedAgents,
                                                            IntPtr assignedTasks);
}
