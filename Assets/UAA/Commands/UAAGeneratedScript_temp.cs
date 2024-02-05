using System.IO;
using UnityEngine;
using UnityEditor;

public class UAAGeneratedScript_temp : EditorWindow
{
    [MenuItem("Edit/UAA - Unity AI Assistant/Execute")]
    private static void DoTask()
    {
        // This is how you can creating a primitive objects:
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // the available  objects are: Sphere, Capsule, Cylinder, Cube, Plane, and Quad.

        // If you want to change anything related to material, create one first like this:
        Material mat = new(Shader.Find("Universal Render Pipeline/Lit"));
        // !! And DO NOT Get the MeshRenderer and change it's properties
        cube.GetComponent<MeshRenderer>().material.color = Color.green;
    }
}