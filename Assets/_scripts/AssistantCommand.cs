using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


public static class AssistantCommand
{
    static private List<string> _tasks = new List<string>();
    private const string TempFilePath = "Assets/UnityLMForge/GeneratedScriptTemp.cs";

    static bool TempFileExists => System.IO.File.Exists(TempFilePath);

    static private string _simplifyCommandToTasksPrompt = Prompts.SimplifyCommandToTasksPrompt;


    public static async void InitializeCommand(string prompt)
    {
        //_tasks = await SimplifyCommand(prompt);

        // to simplify things let's not separate the tasks for now
        await Task.Delay(0);
        _tasks = new List<string> { prompt };
        //

        HandleTasks();
    }

    
    private static  async Task<List<string>> SimplifyCommand(string commandPrompt)
    {
        commandPrompt = _simplifyCommandToTasksPrompt + commandPrompt;

        LLMInput llmInput = LLMConnection.CreateLLMInput(Prompts.SimplifyCommandToTasksPrompt, commandPrompt);

        string tasks = "";
        await LLMConnection.SendAndReceiveStreamedMessages(llmInput, (response) =>
        {
            tasks = response;
            Debug.Log(response);
        });

        return SeparateTasks(tasks);
    }

    private static List<string> SeparateTasks(string input)
    {
        var tasks = new List<string>();
        var matches = Regex.Matches(input, @"- Task \d+: [^.]*\.");

        foreach (Match match in matches)
        {
            tasks.Add(match.Value);
        }

        return tasks;
    }


    private static async void HandleTasks()
    {

        LLMInput llmInput = LLMConnection.CreateLLMInput(Prompts.CommandToScriptPrompt, "AI command script:" + _tasks[0]);

        string script = "";
        await LLMConnection.SendAndReceiveStreamedMessages(llmInput, (generatedScript) =>
        {
            script = generatedScript;
            LLMChatBot.GeneratedString = script;
        });

        script = CleanupScript(script);
        CreateScriptAsset(script);
    }

    private static string CleanupScript(string script)
    {
        if (!script.Contains("```csharp"))
            return script;
        
        var match = Regex.Match(script, @"```csharp(.*?)```", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static void CreateScriptAsset(string code)
    {
        // UnityEditor internal method: ProjectWindowUtil.CreateScriptAssetWithContent
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var method = typeof(ProjectWindowUtil).GetMethod("CreateScriptAssetWithContent", flags);
        method.Invoke(null, new object[] { TempFilePath, code });
    }

    public static void ValidateStep()
    {
        if (!TempFileExists) return;
        EditorApplication.ExecuteMenuItem("Edit/Do Task");
        //AssetDatabase.DeleteAsset(TempFilePath);
    }
}
