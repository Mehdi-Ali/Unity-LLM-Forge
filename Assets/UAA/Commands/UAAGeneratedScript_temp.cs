using UnityEngine;
using UnityEditor;

public class UAAScript : EditorWindow
{
    [MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPosition = Random.insideUnitSphere * 5f;
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = randomPosition;
        }
    }
}