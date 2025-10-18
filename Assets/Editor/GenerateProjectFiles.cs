using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to generate C# project files for CI/CD linting.
/// Called via: Unity -quit -batchmode -executeMethod GenerateProjectFiles.Generate
///
/// Uses ExecuteMenuItem to trigger Unity's built-in project file generation.
/// This works from batch mode and doesn't attempt to shell execute the files.
/// Based on: https://discussions.unity.com/t/any-way-to-tell-unity-to-generate-the-sln-file-via-script-or-command-line/620725
/// </summary>
public static class GenerateProjectFiles
{
    [MenuItem("Tools/Generate Project Files")]
    public static void Generate()
    {
        Debug.Log("Generating C# project files...");

        // Trigger Unity's built-in project file generation
        // This menu item creates .sln and .csproj files
        // In batch mode, it won't attempt to open an external editor
        EditorApplication.ExecuteMenuItem("Assets/Open C# Project");

        Debug.Log("Project files generated successfully.");
    }
}
