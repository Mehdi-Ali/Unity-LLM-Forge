using UnityEngine;
using UnityEditor;
using System.Linq;

public class GeneratedScript : EditorWindow
{
    [MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        GameObject[] cubes = FindObjectsOfType<GameObject>()
            .Where(go => go.name == "Cube")
            .ToArray();

        foreach (GameObject cube in cubes)
        {
            DestroyImmediate(cube, true);
        }
    }
}