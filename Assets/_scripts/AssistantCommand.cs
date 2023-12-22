using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssistantCommand : MonoBehaviour
{

    private const string TempFilePath = "Assets/_scripts/AssistantCommand.cs";


    void CreateScriptAsset(string code)
    {
        // // UnityEditor internal method: ProjectWindowUtil.CreateScriptAssetWithContent
        // var flags = BindingFlags.Static | BindingFlags.NonPublic;
        // var method = typeof(ProjectWindowUtil).GetMethod("CreateScriptAssetWithContent", flags);
        // method.Invoke(null, new object[] { TempFilePath, code });
    }
}
