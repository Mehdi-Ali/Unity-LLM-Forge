using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class UAAGeneratedScript : EditorWindow
{
    [MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < 10; i++)
        {
            Vector3 position;
            do
            {
                position.x = Random.Range(-5f, 5f);
                position.y = Random.Range(0.5f, 2.5f);
                position.z = Random.Range(-5f, 5f);
            } while (Physics.CheckSphere(position, 1f));
            positions.Add(position);
        }

        for (int i = 0; i < 10; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = positions[i];
        }
    }
}