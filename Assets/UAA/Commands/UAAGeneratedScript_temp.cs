using UnityEngine;
using UnityEditor;

public class UAAGeneratedScript : EditorWindow
{
    [MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        Vector3 position;
        for (int i = 0; i < 10; i++)
        {
            position = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            GameObject sphere = UnityEngine.Object.Instantiate(UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Sphere), position, Quaternion.identity);
        }
    }
}
