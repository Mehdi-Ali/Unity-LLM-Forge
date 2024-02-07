using UnityEditor;
using UnityEngine;

public class UAAScriptGuid : MonoBehaviour
{
    private static void Examples()
    {
        // This is how you can creating a primitive objects:
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // the available  objects are: Sphere, Capsule, Cylinder, Cube, Plane, and Quad.

        // If you want to change anything related to material, create one first like this:
        Material mat = new(Shader.Find("Universal Render Pipeline/Lit"));
        // !! And DO NOT Get the MeshRenderer and change it's properties
        cube.GetComponent<MeshRenderer>().material.color = Color.green;

        // This is how you can create a prefabs in the scene
        var prefabPath = "Assets/Resources_moved/Prefabs/Prefab.prefab";
        UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
        GameObject player = Instantiate(prefab) as GameObject;
    }
}

