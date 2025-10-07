using System;
using System.Runtime.InteropServices;

public static class Assignment {
  // Assign the agents to the tasks using a cover assignment.
  [DllImport("assignment")]
  public static extern Plugin.StatusCode Assignment_CoverAssignment_Assign(
      int numAgents, int numTasks, float[] costs, int[] assignedAgents, int[] assignedTasks,
      out int numAssignments);

  // Assign the agents to the tasks using an even assignment.
  [DllImport("assignment")]
  public static extern Plugin.StatusCode Assignment_EvenAssignment_Assign(
      int numAgents, int numTasks, float[] costs, int[] assignedAgents, int[] assignedTasks,
      out int numAssignments);

  // Assign the agents to the tasks using a weighted even assignment.
  [DllImport("assignment")]
  public static extern Plugin.StatusCode Assignment_WeightedEvenAssignment_Assign(
      int numAgents, int numTasks, float[] costs, float[] weights, int weightScalingFactor,
      int[] assignedAgents, int[] assignedTasks, out int numAssignments);
}
