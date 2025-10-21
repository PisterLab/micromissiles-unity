using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Ensures analyzer DLLs under Assets/Analyzers are correctly configured on every load/import.
static class AnalyzerAutoSetup
{
    const string AnalyzerRoot = "Assets/Analyzers";
    static readonly string[] AnalyzerSearch = {"*.dll"};

    [InitializeOnLoadMethod]
    static void Init() => ApplySettings();

    [MenuItem("Tools/Analyzers/Reapply Import Settings")]
    static void ApplySettings()
    {
        if (!Directory.Exists(AnalyzerRoot))
            return;

        var dlls = AnalyzerSearch.SelectMany(p => Directory.GetFiles(AnalyzerRoot, p, SearchOption.AllDirectories)).ToArray();
        foreach (var path in dlls)
        {
            var relPath = path.Replace(Path.DirectorySeparatorChar, '/');
            var importer = AssetImporter.GetAtPath(relPath) as PluginImporter;
            if (importer == null)
                continue;

            // Mark as Roslyn Analyzer via asset label.
            var obj = AssetDatabase.LoadMainAssetAtPath(relPath);
            if (obj != null)
            {
                var labels = AssetDatabase.GetLabels(obj).ToList();
                if (!labels.Contains("RoslynAnalyzer"))
                {
                    labels.Add("RoslynAnalyzer");
                    AssetDatabase.SetLabels(obj, labels.ToArray());
                }
            }

            // Keep analyzers out of runtime/editor plugin load lists.
            try
            {
                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(false);
                foreach (BuildTarget t in Enum.GetValues(typeof(BuildTarget)))
                {
                    try { importer.SetCompatibleWithPlatform(t, false); }
                    catch { /* some targets may be invalid on current editor */ }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AnalyzerAutoSetup: Could not adjust importer for {relPath}: {e.Message}");
            }

            importer.SaveAndReimport();
        }

        AssetDatabase.Refresh();
        Debug.Log($"AnalyzerAutoSetup: configured {dlls.Length} analyzer DLL(s).");
    }
}

