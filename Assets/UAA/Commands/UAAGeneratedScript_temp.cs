using UnityEngine;
using UnityEditor;

public class UAAGeneratedScript : EditorWindow
{
    [MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        GameObject[] spheres = FindObjectsOfType(typeof(GameObject), FindObjectsRecursively);
        GameObject floor = FindObjectOfType<GameObject>(FindMode.Deep);

        foreach (GameObject sphere in spheres)
        {
            if (floor != null && sphere != null)
            {
                Vector3 currentPosition = sphere.transform.position;
                float distanceToFloor = Vector3.Distance(currentPosition, floor.transform.position);
                Vector3 newPosition = currentPosition + new Vector3(0f, distanceToFloor, 0f);
                sphere.transform.position = newPosition;
            }
        }
    }
}