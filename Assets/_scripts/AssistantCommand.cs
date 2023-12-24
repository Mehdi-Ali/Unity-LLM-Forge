using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;


public class AssistantCommand
{
    static private List<string> _tasks = new List<string>();
    private const string TempFilePath = "Assets/UnityLMForge/Commands/GeneratedScript_temp.cs";

    static bool TempFileExists => System.IO.File.Exists(TempFilePath);
    static string script = "";

    static private string _simplifyCommandToTasksPrompt = Prompts.SimplifyCommandToTasksPrompt;

    static private LocalLLMInput llmInput;

    private static StringBuilder _errorContent = new StringBuilder();

    private static bool _isCorrectingScript = false;



    static AssistantCommand()
    {
        Application.logMessageReceived += HandleLog;
        AssemblyReloadEvents.afterAssemblyReload += CheckForIDEErrors;

    }

    public static void UnsubscribeFromEvents()
    {
        Application.logMessageReceived -= HandleLog;
        AssemblyReloadEvents.afterAssemblyReload -= CheckForIDEErrors;
    }

    private static void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            _errorContent.AppendLine(logString);
        }
    }

    public static async void InitializeCommand(string prompt)
    {
        LLMChatBot.GeneratedString = "coding...";
        //_tasks = await SimplifyCommand(prompt);

        // to simplify things let's not separate the tasks for now
        await Task.Delay(0);
        HandleTask(prompt);
    }


    private static async Task<List<string>> SimplifyCommand(string commandPrompt)
    {
        commandPrompt = _simplifyCommandToTasksPrompt + commandPrompt;

        llmInput = LLMConnection.CreateLLMInput(Prompts.SimplifyCommandToTasksPrompt, commandPrompt);
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


    private static async void HandleTask(string task)
    {
        llmInput = LLMConnection.CreateLLMInput(Prompts.TaskToScriptPrompt, "The task is described as follows:\n" + task);
        await CreateScript();
        _isCorrectingScript = true;
    }

    private static async void CheckForIDEErrors()
    {
        Debug.Log("CheckForIDEErrors");
        Debug.Log(_isCorrectingScript);
        
        if (!_isCorrectingScript)
            return;
        
        ClearLog();
        Debug.Log("ErrorContent: " + _errorContent);

        if (_errorContent.Length > 0)
            await CorrectScript();

        ExecuteScript();
    }
    private static async void CheckForRuntimeErrors()
    {
        Debug.Log("CheckForRuntimeErrors");

        Debug.Log("ErrorContent: " + _errorContent);

        if (_errorContent.Length > 0)
            await CorrectScript();

        _isCorrectingScript = true;
        CheckForIDEErrors();
    }

    private static async Task CreateScript(bool isUpdatingScript = false)
    {
        await LLMConnection.SendAndReceiveStreamedMessages(llmInput, (generatedScript) =>
        {
            script = generatedScript;
            LLMChatBot.GeneratedString = script;
        });

        script = CleanupScript(script);
        LLMChatBot.GeneratedString = script;
        CreateScriptAsset(script, isUpdatingScript);
    }

    private static string CleanupScript(string script)
    {
        if (!script.Contains("```csharp"))
            return script;

        var match = Regex.Match(script, @"```csharp(.*?)```", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static void CreateScriptAsset(string code, bool isUpdatingScript)
    {
        if (isUpdatingScript)
            AssetDatabase.DeleteAsset(TempFilePath);

        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var method = typeof(ProjectWindowUtil).GetMethod("CreateScriptAssetWithContent", flags);
        method.Invoke(null, new object[] { TempFilePath, code });
    }

    public static void ExecuteScript(bool isUpdatingScript = false)
    {
        _isCorrectingScript = false;
        if (isUpdatingScript)
            CreateScriptAsset(LLMChatBot.GeneratedString, isUpdatingScript);

        if (!TempFileExists) return;
        EditorApplication.ExecuteMenuItem("Edit/Do Task");

        CheckForRuntimeErrors();
    }

    public static void DeleteGeneratedScript()
    {
        AssetDatabase.DeleteAsset(TempFilePath);
    }

    public static async Task CorrectScript()
    {
        llmInput.messages.Add(new Message
        {
            role = Role.user.ToString(),
            content = "Here's Unity's Console Error Logs :\n" +
                        _errorContent.ToString() +
                        "\n\nPlease correct the script and send it again."
        });

        LLMChatBot.GeneratedString = "Correcting...";
        await CreateScript(true);
    }

    public static void ClearLog()
    {
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
}