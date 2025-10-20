#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

public static class GenerateSolutionFiles
{
    public static void Generate()
    {
        Debug.Log("Forcing solution file generation...");

        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var syncSteps = new Func<bool>[]
        {
            SyncViaRiderReflection,
            SyncViaCurrentCodeEditor,
            SyncViaSyncVSReflection,
            SyncViaMenuItem
        };

        var executed = false;

        foreach (var step in syncSteps)
        {
            try
            {
                if (!step())
                {
                    continue;
                }

                if (!HasSolution(projectRoot))
                {
                    Debug.Log($"Strategy {step.Method.Name} executed but no solution detected yet; continuing to next strategy.");
                    continue;
                }

                executed = true;
                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Solution sync strategy {step.Method.Name} failed: {ex}");
            }
        }

        if (!executed)
        {
            Debug.LogError("All solution sync strategies failed. Solution files may be missing.");
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var slnFiles = Directory.GetFiles(projectRoot, "*.sln", SearchOption.TopDirectoryOnly);
        if (slnFiles.Length == 0)
        {
            Debug.LogWarning("Solution sync completed but no .sln files were found.");
        }
        else
        {
            foreach (var sln in slnFiles)
            {
                Debug.Log($"Generated solution file: {sln}");
            }
        }

        Debug.Log("Solution generation complete!");
    }

    private static bool SyncViaRiderReflection()
    {
        var riderType = Type.GetType("Packages.Rider.Editor.RiderScriptEditor, Unity.Rider.Editor");
        if (riderType == null)
        {
            Debug.Log("RiderScriptEditor type not found (package not installed?).");
            return false;
        }

        var method = riderType.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            Debug.Log("RiderScriptEditor.SyncSolution method unavailable.");
            return false;
        }

        method.Invoke(null, null);
        Debug.Log("Synced via RiderScriptEditor.SyncSolution reflection call.");
        return true;
    }

    private static bool SyncViaCurrentCodeEditor()
    {
        var editor = CodeEditor.CurrentEditor;
        if (editor == null)
        {
            Debug.Log("No registered external code editor - skipping CodeEditor.CurrentEditor.SyncAll.");
            return false;
        }

        editor.SyncAll();
        Debug.Log($"Synced via CodeEditor.CurrentEditor: {editor.GetType().Name}");
        return true;
    }

    private static bool SyncViaSyncVSReflection()
    {
        var unityEditorAssembly = typeof(Editor).Assembly;
        var syncVsType = unityEditorAssembly.GetType("UnityEditor.SyncVS");
        if (syncVsType == null)
        {
            Debug.Log("UnityEditor.SyncVS type not found via reflection.");
            return false;
        }

        var method = syncVsType.GetMethod("SyncSolution", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null)
        {
            Debug.Log("UnityEditor.SyncVS.SyncSolution method unavailable.");
            return false;
        }

        method.Invoke(null, null);
        Debug.Log("Synced via UnityEditor.SyncVS.SyncSolution reflection call.");
        return true;
    }

    private static bool SyncViaMenuItem()
    {
        const string menuItem = "Assets/Open C# Project";
        if (!EditorApplication.ExecuteMenuItem(menuItem))
        {
            Debug.Log("EditorApplication.ExecuteMenuItem returned false.");
            return false;
        }

        Debug.Log($"Synced via EditorApplication.ExecuteMenuItem: {menuItem}");
        return true;
    }

    private static bool HasSolution(string projectRoot)
    {
        return Directory.GetFiles(projectRoot, "*.sln", SearchOption.TopDirectoryOnly).Length > 0;
    }
}
#endif
