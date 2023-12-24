using UnityEditor;
using UnityEngine;

public class MyEditorScript
{
    [MenuItem("Edit/Do Task")]
    public static void DoTask()
    {
        for (int i = 0; i < 100; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5));
        }
    }
}