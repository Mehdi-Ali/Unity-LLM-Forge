using UnityEditor;
using UnityEngine;

public class CreateCubes : Editor
{
    [MenuItem("Edit/Do Task")]
    public static void DoTask()
    {
        GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube1.transform.position = new Vector3(0, 0, 0);

        GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube2.transform.position = new Vector3(1, 0, 0);

        GameObject cube3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube3.transform.position = new Vector3(-1, 0, 0);
    }
}