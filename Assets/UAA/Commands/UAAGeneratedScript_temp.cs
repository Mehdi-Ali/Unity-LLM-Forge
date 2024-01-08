using UnityEngine;
using UnityEditor;

public class UAAGeneratedScript : EditorWindow
{
    [MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        Vector3[] positions = new Vector3[10];
        for (int i = 0; i < 10; i++)
        {
            positions[i] = Random.insideUnitSphere * 5f;
        }
        GameObject[] spheres = new GameObject[10];
        for (int i = 0; i < 10; i++)
        {
            spheres[i] = UnityEngine.Object.Instantiate(Resources.Load("StandardSphere"), positions[i], Quaternion.identity) as GameObject;
        }
    }
}