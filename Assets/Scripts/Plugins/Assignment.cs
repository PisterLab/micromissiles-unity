using System;
using System.Runtime.InteropServices;

public class Assignment {
  // Assign the agents to the tasks using a cover assignment.
  [DllImport("libassignment")]
  public static extern int Assignment_CoverAssignment_Assign(int numAgents, int numTasks,
                                                             float[] costs, IntPtr assignedAgents,
                                                             IntPtr assignedTasks);

  // Assign the agents to the tasks using an even assignment.
  [DllImport("libassignment")]
  public static extern int Assignment_EvenAssignment_Assign(int numAgents, int numTasks,
                                                            float[] costs, IntPtr assignedAgents,
                                                            IntPtr assignedTasks);
}
