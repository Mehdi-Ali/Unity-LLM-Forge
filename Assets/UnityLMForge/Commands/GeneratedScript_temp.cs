using UnityEditor;
using UnityEngine;

public class GeneratedScript : MonoBehaviour
{
    [MenuItem("Edit/Do Task")]
    public static void DoTask()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPosition = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            GameObject sphere = GameObject.Instantiate(Resources.Load("Sphere")) as GameObject;
            sphere.transform.position = randomPosition;
        }
    }
}