using UnityEditor;
using UnityEngine;

public class GeneratedScript_temp : MonoBehaviour {
    [MenuItem("Edit/Do Task")]
    public static void DoTask {
        for (int i = 0; i < 10; i++) {
            Vector3 pos = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f });
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = pos;
        }
    }
}