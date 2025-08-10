using System;
using System.Runtime.InteropServices;

public static class Assignment {
  // Assign the agents to the tasks using a cover assignment.
  [DllImport("assignment")]
  public static extern int Assignment_CoverAssignment_Assign(int numAgents, int numTasks,
                                                             float[] costs, IntPtr assignedAgents,
                                                             IntPtr assignedTasks);

  // Assign the agents to the tasks using an even assignment.
  [DllImport("assignment")]
  public static extern int Assignment_EvenAssignment_Assign(int numAgents, int numTasks,
                                                            float[] costs, IntPtr assignedAgents,
                                                            IntPtr assignedTasks);

  // Assign the agents to the tasks using a weighted even assignment.
  [DllImport("assignment")]
  public static extern int Assignment_WeightedEvenAssignment_Assign(int numAgents, int numTasks,
                                                                    float[] costs, float[] weights,
                                                                    int weightScalingFactor,
                                                                    IntPtr assignedAgents,
                                                                    IntPtr assignedTasks);
}
