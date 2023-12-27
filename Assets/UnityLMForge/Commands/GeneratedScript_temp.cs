using UnityEngine;
using UnityEditor;

public class GeneratedScript : EditorWindow
{
    [MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 position = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            GameObject cube = UnityEngine.Object.Instantiate(Resources.Load("Cube")) as GameObject;
            cube.transform.position = position;
        }
    }
}