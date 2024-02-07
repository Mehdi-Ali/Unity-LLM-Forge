using UnityEngine;
using UnityEditor;

public class UAAGeneratedScript_temp : EditorWindow
{
    [MenuItem("Edit/UAA - Unity AI Assistant/Execute")]
    private static void DoTask()
    {
        // Create a room using walls and wood planks
        var wallPath = "Assets/Resources_moved/Prefabs/Wall_04.prefab";
        var woodPlankPath = "Assets/Resources_moved/Prefabs/WoodPlank_01.prefab";
        
        // Load the prefabs from the paths
        UnityEngine.Object wallPrefab = AssetDatabase.LoadAssetAtPath(wallPath, typeof(GameObject));
        UnityEngine.Object woodPlankPrefab = AssetDatabase.LoadAssetAtPath(woodPlankPath, typeof(GameObject));
        
        // Instantiate the prefabs in the scene
        GameObject wall1 = Instantiate(wallPrefab) as GameObject;
        GameObject woodPlank1 = Instantiate(woodPlankPrefab) as GameObject;
        
        // Position and rotate the objects
        wall1.transform.position = new Vector3(-5, 0, -5);
        wall1.transform.rotation = Quaternion.Euler(0, 90, 0);
        woodPlank1.transform.position = new Vector3(-2, 0, -2);
        woodPlank1.transform.rotation = Quaternion.Euler(0, 90, 0);
    }
}