#if UNITY_EDITOR
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

public static class GenerateSolutionFiles
{
    public static void Generate()
    {
        Debug.Log("Forcing solution file generation...");

        var editor = CodeEditor.CurrentEditor;
        if (editor != null)
        {
            editor.SyncAll();
            Debug.Log($"Synced external code editor: {editor.GetType().Name}");
        }
        else
        {
            Debug.LogWarning("No external code editor is registered; skipping solution sync.");
        }

        Debug.Log("Solution generation complete!");
    }
}
#endif
