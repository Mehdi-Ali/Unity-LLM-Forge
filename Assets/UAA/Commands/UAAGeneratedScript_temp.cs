using UnityEngine;
using UnityEditor;

public class UAAGeneratedScript : EditorWindow
{
    [MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject spherePrefab = Resources.Load<GameObject>("StandardSphere");
            if(spherePrefab == null) continue; // check if prefab is not null before instantiating
            Vector3 randomPosition = new Vector3(Random.Range(-5f, Random.Range(-5f, 5f)), Random.Range(-5f, Random.Range(-5f, 5f)), Random.Range(-5f, Random.Range(-5f, 5f)));
            GameObject sphere = UnityEngine.Object.Instantiate(spherePrefab, randomPosition, Quaternion.identity);
        }
    }
}