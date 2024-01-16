using UnityEngine;
using UnityEditor;

public class UAAGeneratedScript_temp : EditorWindow
{
    [MenuItem("Edit/Do Task")]
    private static void DoTask()
    {
        // create a plane gameobject with the name "Plane"
        GameObject plane = new GameObject("Plane");
        // set its position to (0, 0, -0.1)
        plane.transform.position = new Vector3(0f, 0f, -0.1f);
        // set its scale to (10, 1, 10)
        plane.transform.localScale = new Vector3(10f, 1f, 10f);
        // set it as a Quad mesh
        plane.AddComponent<MeshFilter>().mesh = new Mesh();
        plane.GetComponent<MeshFilter>().mesh.SetVertices(new Vector3[] {
            new Vector3(-5f, 0f, -5f),
            new Vector3(5f, 0f, -5f),
            new Vector3(5f, 0f, 5f),
            new Vector3(-5f, 0f, 5f),
        });
        plane.GetComponent<MeshFilter>().mesh.SetTriangles(new int[] {
            0, 1, 2, // front
            0, 2, 3, // right
            0, 3, 1, // left
            1, 3, 2 // back
        }, 0);
        plane.GetComponent<MeshFilter>().mesh.RecalculateNormals();
        plane.AddComponent<MeshRenderer>();
        plane.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
        // create 3 more planes as walls
        for (int i = 0; i < 3; i++)
        {
            GameObject wall = new GameObject("Plane_Wall");
            wall.transform.position = new Vector3(i * 10f, 0f, -5f);
            wall.AddComponent<MeshFilter>().mesh = plane.GetComponent<MeshFilter>().mesh;
            wall.AddComponent<MeshRenderer>().material = plane.GetComponent<MeshRenderer>().material;
        }
    }
}