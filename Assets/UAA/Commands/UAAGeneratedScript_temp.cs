using UnityEngine;
using UnityEditor;

public class UAAGeneratedScript_temp : EditorWindow
{
    [MenuItem("Edit/UAA - Unity AI Assistant/Execute")]
    private static void DoTask()
    {
        // Add fog effect to the scene
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.5f, 0.5f, 0.5f);
        RenderSettings.fogDensity = 0.01f;
    }
}