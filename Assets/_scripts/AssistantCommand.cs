using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using System;


public class AssistantCommand : MonoBehaviour
{

    private const string TempFilePath = "Assets/_scripts/AssistantCommand.cs";
    private Queue<string> _tasks = new Queue<string>();

    const string simplifyCommandToTasksPrompt = "Your are.. simplify.. in this format... ";

    void InitializeCommand(string prompt)
    {
        _tasks = SimplifyCommand(prompt);
        CreateScriptAsset(prompt);
    }

    private Queue<string> SimplifyCommand(string prompt)
    {
        prompt = simplifyCommandToTasksPrompt + prompt;
        // run llmChat with this prompt and get the response
        var tasks = new Queue<string>();



        return tasks;
    }

    void CreateScriptAsset(string code)
    {
        // UnityEditor internal method: ProjectWindowUtil.CreateScriptAssetWithContent
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var method = typeof(ProjectWindowUtil).GetMethod("CreateScriptAssetWithContent", flags);
        method.Invoke(null, new object[] { TempFilePath, code });
    }
}
