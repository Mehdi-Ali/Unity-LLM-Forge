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
    public static ToolLifecycleManagerSO LifecycleManager;

    static private List<string> _tasks = new List<string>();
    private const string TempFilePath = "Assets/UnityLMForge/Commands/GeneratedScript_temp.cs";

    static bool TempFileExists => System.IO.File.Exists(TempFilePath);
    static string script = "";

    static private string _simplifyCommandToTasksPrompt = Prompts.SimplifyCommandToTasksPrompt;

    public static LocalLLMInput LLMInput { get => LLMAssistant.LifecycleManager.CachedLLMInput; set => LLMAssistant.LifecycleManager.CachedLLMInput = value; }
    public static LifecycleState Lifecycle { get => LifecycleManager.Lifecycle; set => LifecycleManager.Lifecycle = value; }

    private static StringBuilder _errorContent = new StringBuilder();



    static AssistantCommand()
    {
        Application.logMessageReceived += SaveLogMessages;
        AssemblyReloadEvents.afterAssemblyReload += CheckForIDEErrors;

        if (LifecycleManager == null)
            LifecycleManager = AssetDatabase.LoadAssetAtPath<ToolLifecycleManagerSO>("Assets/UnityLMForge/ToolLifecycleManager.asset");
    }

    public static void UnsubscribeFromEvents()
    {
        Application.logMessageReceived -= SaveLogMessages;
        AssemblyReloadEvents.afterAssemblyReload -= CheckForIDEErrors;
    }

    private static void SaveLogMessages(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            _errorContent.AppendLine(logString);
        }
    }

    [InitializeOnLoadMethod]
    public static async void Resume()
    {
        await Task.Delay(1000);
        Debug.Log("Resume");
        switch (Lifecycle)
        {
            case LifecycleState.FixingIDErrors:
                CheckForIDEErrors();
                break;
            case LifecycleState.FixingRuntimeErrors:
                CheckForRuntimeErrors();
                break;
            case LifecycleState.idle:
                return;
        }
    }

    public static async void InitializeCommand(string prompt)
    {
        //_tasks = await SimplifyCommand(prompt);
        // to simplify things let's not separate the tasks for now

        LLMAssistant.GeneratedString = "coding...";
        await Task.Delay(0);
        HandleTask(prompt);
    }

    private static async Task<List<string>> SimplifyCommand(string commandPrompt)
    {
        commandPrompt = _simplifyCommandToTasksPrompt + commandPrompt;

        LLMInput = LLMConnection.CreateLLMInput(Prompts.SimplifyCommandToTasksPrompt, commandPrompt);
        string tasks = "";
        await LLMConnection.SendAndReceiveStreamedMessages(LLMInput, (response) =>
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
        LLMInput = LLMConnection.CreateLLMInput(Prompts.TaskToScriptPrompt, "The task is described as follows:\n" + task);
        await CreateScript();
    }

    private static async Task CreateScript(bool isUpdatingScript = false)
    {
        await LLMConnection.SendAndReceiveStreamedMessages(LLMInput, (generatedScript) =>
        {
            LLMAssistant.GeneratedString = generatedScript;
        });

        LLMAssistant.GeneratedString = GetOnlyScript(LLMAssistant.GeneratedString);
        script = LLMAssistant.GeneratedString;

        Lifecycle = LifecycleState.FixingIDErrors;
        CreateScriptAsset(script, isUpdatingScript);
    }

    private static string GetOnlyScript(string script)
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

    public static void ExecuteScript()
    {
        if (!TempFileExists)
            return;

        Lifecycle = LifecycleState.FixingRuntimeErrors;

        EditorApplication.ExecuteMenuItem("Edit/Do Task");
        Resume();
    }

    public static void DeleteGeneratedScript()
    {
        AssetDatabase.DeleteAsset(TempFilePath);
    }

    //[InitializeOnLoadMethod]
    private static async void CheckForIDEErrors()
    {
        Debug.Log("Check For IDE Errors");
        if (_errorContent.Length > 0)
            await CorrectScript();
        else
        {
            Lifecycle = LifecycleState.idle;
            ExecuteScript();
        }
    }
    private static async void CheckForRuntimeErrors()
    {
        Debug.Log("Check For Runtime Errors");
        if (_errorContent.Length > 0)
            await CorrectScript();
        else
        {
            Lifecycle = LifecycleState.idle;
        }
    }


    public static async Task CorrectScript()
    {

        LLMInput.messages.Add(new Message
        {
            role = Role.assistant.ToString(),
            content = "```csharp\n" + LLMAssistant.GeneratedString + "\n```"
        });

        LLMInput.messages.Add(new Message
        {
            role = Role.user.ToString(),
            content = "Here's Unity's Console Error Logs :\n" +
                        _errorContent.ToString() +
                        Prompts.CorrectScriptPrompt
        });

        _errorContent.Clear();
        LLMAssistant.GeneratedString = "Correcting...";
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