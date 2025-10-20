using UnityEditor;
using UnityEngine;

public static class GenerateSolutionFiles
{
    public static void Generate()
    {
        Debug.Log("Forcing solution file generation...");

        // This syncs all code projects
        UnityEditor.SyncVS.SyncSolution();

        Debug.Log("Solution generation complete!");
    }
}
