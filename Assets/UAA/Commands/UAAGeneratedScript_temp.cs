using UnityEngine;
using UnityEditor;

public class UAAGeneratedScript_temp : EditorWindow
{
    [MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        Transform parent = FindObjectOfType<Transform>(); // find the root transform to attach the spheres as children
        if (parent == null) return; // exit if no root transform is found

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPosition = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)); // generate a random position within the scene bounds
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); // create a new sphere game object
            sphere.transform.position = randomPosition; // set the sphere's position to the randomly generated one
            sphere.transform.SetParent(parent); // attach the sphere as a child of the root transform
        }
    }
}