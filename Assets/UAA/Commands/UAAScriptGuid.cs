using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UAAScriptGuid : MonoBehaviour
{
    private static void Examples()
    {
        // an example of creating a primitive cube:
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // an example of creating a new material and setting it's color to red
        Material mat = new(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = Color.red
        };

        // an example of setting the material of a game object
        cube.GetComponent<Renderer>().material = mat;
    }
}

