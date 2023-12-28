using UnityEngine;
using UnityEditor;

public class UAAScriptTemplate : EditorWindow
{
    //[MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        // an example of creating a primitive cube:
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    }
}