using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
public class GenerateCone : EditorWindow {
  private int sides = 16;
  private float baseRadius = 1f;
  private float height = 2f;

  [MenuItem("GameObject/3D Object/Cone", false, 10)]
  static void CreateCone() {
    GameObject cone;
    GameObject selectedObject = Selection.activeGameObject;

    if (selectedObject != null) {
      // Create as child of selected object
      cone = new GameObject("Cone");
      cone.transform.SetParent(selectedObject.transform, false);
    } else {
      // Create as new root object
      cone = new GameObject("Cone");
    }

    cone.AddComponent<MeshFilter>();
    cone.AddComponent<MeshRenderer>();
    Undo.RegisterCreatedObjectUndo(cone, "Create Cone");

    var window = ScriptableObject.CreateInstance<GenerateCone>();
    window.GenerateConeObject(cone);

    Selection.activeGameObject = cone;
  }

  void GenerateConeObject(GameObject cone) {
    Mesh mesh =
        CreateConeMesh("ConeMesh", sides, Vector3.zero, Quaternion.identity, baseRadius, height);

    // Save the mesh as an asset
    string path = "Assets/Meshes";
    if (!AssetDatabase.IsValidFolder(path)) {
      AssetDatabase.CreateFolder("Assets", "Meshes");
    }
    string assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/ConeMesh.asset");
    AssetDatabase.CreateAsset(mesh, assetPath);
    AssetDatabase.SaveAssets();

    // Assign the mesh to the MeshFilter
    cone.GetComponent<MeshFilter>().sharedMesh = mesh;
    cone.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
  }

  Vector2[] GetBasePoints(int vertices, float radius) {
    const float TAU = 2f * Mathf.PI;
    var pts = new Vector2[vertices];
    var step = TAU / vertices;  // angular step between two vertices
    for (int i = 0; i < vertices; i++) {
      pts[i] = radius * Trig(i * step);  // convert polar coordinate to cartesian space
    }
    return pts;
  }

  static Vector2 Trig(float rad) => new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

  Vector3[] BuildConeVertices(Vector2[] baseVerts, float coneHeight) {
    if (baseVerts == null || baseVerts.Length < 3)
      throw new InvalidOperationException("Requires at least 3 base vertices.");
    var verts = new Vector3[baseVerts.Length + 1];
    verts[0] = new Vector3(0f, coneHeight, 0f);
    for (int i = 0; i < baseVerts.Length; i++) {
      verts[i + 1] = new Vector3(baseVerts[i].x, 0f, baseVerts[i].y);
    }
    return verts;
  }

  void ConstructCone(Vector3[] coneVerts, List<Vector3> finalVerts, List<int> triangles) {
    if (coneVerts == null || coneVerts.Length < 4)
      throw new InvalidOperationException("Requires at least 4 vertices.");
    if (finalVerts == null || triangles == null)
      throw new ArgumentNullException();

    finalVerts.Clear();
    triangles.Clear();

    var rimVertices = coneVerts.Length - 1;

    // Side faces
    for (int i = 1; i <= rimVertices; i++) {
      int a = i, b = i < rimVertices ? i + 1 : 1;
      AddTriangle(coneVerts[0], coneVerts[b], coneVerts[a]);
    }

    // Base face
    for (int i = 1; i < rimVertices - 1; i++) {
      AddTriangle(coneVerts[1], coneVerts[i + 1], coneVerts[i + 2]);
    }

    void AddTriangle(Vector3 t1, Vector3 t2, Vector3 t3) {
      finalVerts.Add(t1);
      finalVerts.Add(t2);
      finalVerts.Add(t3);
      triangles.Add(finalVerts.Count - 3);
      triangles.Add(finalVerts.Count - 2);
      triangles.Add(finalVerts.Count - 1);
    }
  }
  Mesh CreateConeMesh(string name, int sides, Vector3 apex, Quaternion rotation, float baseRadius,
                      float height) {
    var baseVerts = GetBasePoints(sides, baseRadius);
    var coneVerts = BuildConeVertices(baseVerts, height);

    var verts = new List<Vector3>();
    var tris = new List<int>();
    ConstructCone(coneVerts, verts, tris);

    for (int i = 0; i < verts.Count; i++) {
      verts[i] = rotation * (verts[i] - coneVerts[0]);
    }

    // Recenter the cone
    Vector3 center = CalculateCenter(verts);
    for (int i = 0; i < verts.Count; i++) {
      verts[i] = verts[i] - center + apex;
    }

    Mesh mesh = new Mesh();
    mesh.name = name;
    mesh.SetVertices(verts);
    mesh.SetTriangles(tris.ToArray(), 0);
    mesh.RecalculateNormals();

    return mesh;
  }

  Vector3 CalculateCenter(List<Vector3> vertices) {
    Vector3 sum = Vector3.zero;
    foreach (Vector3 vert in vertices) {
      sum += vert;
    }
    return sum / vertices.Count;
  }
}
#endif
