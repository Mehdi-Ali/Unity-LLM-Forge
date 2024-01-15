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


namespace UAA
{
    public class UAACommand
    {
        private static UAASettingsSO _settings;
        private static UAASettingsSO Settings
        {
            get
            {
                if (_settings == null)
                    _settings = AssetDatabase.LoadAssetAtPath<UAASettingsSO>("Assets/UAA/Settings/UAASettings.asset");

                return _settings;
            }
        }

        public static bool IsCommandAborted = false;

        public static LocalLLMRequestInput LLMInput
        {
            get => Settings.CachedLLMInput;
            set => Settings.CachedLLMInput = value;
        }

        private static string SimplifyCommandToTasksPrompt
        {
            get => Settings.Prompts.SimplifyCommandToTasksPrompt;
            set => Settings.Prompts.SimplifyCommandToTasksPrompt = value;
        }

        private static string TaskToScriptPrompt
        {
            get => Settings.Prompts.TaskToScriptPrompt;
            set => Settings.Prompts.TaskToScriptPrompt = value;
        }

        private static string CorrectScriptPrompt
        {
            get => Settings.Prompts.CorrectScriptPrompt;
            set => Settings.Prompts.CorrectScriptPrompt = value;
        }

        private static bool IsCorrectingScript
        {
            get => Settings.IsCorrectingScript;
            set => Settings.IsCorrectingScript = value;
        }

        private static CorrectingStates CorrectingState
        {
            get => Settings.CorrectingState;
            set => Settings.CorrectingState = value;
        }

        private static string ErrorLogs
        {
            get => Settings.ErrorLogs;
            set => Settings.ErrorLogs = value;
        }

        private static string TempFilePath => Settings.TempFilePath;
        private static bool TempFileExists => System.IO.File.Exists(TempFilePath);

        public static int MaxIterationsBeforeRestarting => Settings.MaxIterationsBeforeRestarting;

        private static readonly List<string> _tasks = new();
        private static string script = "";


        static UAACommand()
        {
            Application.logMessageReceived += SaveLogMessages;
        }

        private static void SaveLogMessages(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                ErrorLogs += " * " + logString + "\n" + stackTrace + "\n\n";
            }
        }

        [InitializeOnLoadMethod]
        public static async void Resume()
        {           
            await Task.Delay(1000);
            switch (CorrectingState)
            {
                case CorrectingStates.FixingIDErrors:
                    CheckForIDEErrors();
                    break;
                case CorrectingStates.FixingRuntimeErrors:
                    CheckForRuntimeErrors();
                    break;
                case CorrectingStates.NotFixing:
                    return;
            }
        }

        public static async void InitializeCommand()
        {
            CorrectingState = CorrectingStates.NotFixing;
            IsCorrectingScript = false;

            if (string.IsNullOrEmpty(UAAWindow.UserCommandMessage))
                UAAWindow.UserCommandMessage = UAADefaultPrompts.DefaultUserCommandMessage;

            ErrorLogs = null;

            //_tasks = await SimplifyCommand(prompt);
            // to simplify things let's not separate the tasks for now

            UAAWindow.GeneratedString = "coding...";
            await Task.Delay(0);
            HandleTask(UAAWindow.UserCommandMessage);
        }

        private static async Task<List<string>> SimplifyCommand(string commandPrompt)
        {
            LLMInput = UAAConnection.CreateLLMInput(SimplifyCommandToTasksPrompt, commandPrompt);
            string tasks = "";
            await UAAConnection.SendAndReceiveStreamedMessages(LLMInput, (response) =>
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
            LLMInput = UAAConnection.CreateLLMInput(TaskToScriptPrompt, "The task is described as follows:\n" + task);
            await CreateScript();
        }

        private static async Task CreateScript(bool isUpdatingScript = false)
        {

            if (LLMInput.messages.Count > MaxIterationsBeforeRestarting * 2 + 1)
            {
                InitializeCommand();
                return;
            }

            await UAAConnection.SendAndReceiveStreamedMessages(LLMInput, (generatedScript) =>
            {
                UAAWindow.GeneratedString = generatedScript;
            });

            if (IsCommandAborted)
            {
                Debug.Log("Command Stopped");
                IsCommandAborted = false;
                return;
            }

            UAAWindow.GeneratedString = GetOnlyScript(UAAWindow.GeneratedString);
            script = UAAWindow.GeneratedString;

            LLMInput.messages.Add(new Message
            {
                role = Role.assistant.ToString(),
                content = "```csharp\n" + script + "\n```"
            });

            CorrectingState = CorrectingStates.FixingIDErrors;
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

            IsCorrectingScript = false;

            if (TempFileExists)
            {
                var previousCode = System.IO.File.ReadAllText(TempFilePath);
                if (previousCode == code)
                {
                    InitializeCommand();
                    return;
                }
            }

            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var method = typeof(ProjectWindowUtil).GetMethod("CreateScriptAssetWithContent", flags);
            method.Invoke(null, new object[] { TempFilePath, code });
            Resume();
        }

        public static void ExecuteScript()
        {
            if (!TempFileExists)
                return;

            CorrectingState = CorrectingStates.FixingRuntimeErrors;

            EditorApplication.ExecuteMenuItem("Edit/Do Task");
            Resume();
        }

        public static void DeleteGeneratedScript()
        {
            // todo i should achieve instead of completely deleting;
            AssetDatabase.DeleteAsset(TempFilePath);
        }

        private static async void CheckForIDEErrors()
        {
            if (!string.IsNullOrEmpty(ErrorLogs))
                await CorrectScript();
            else
            {
                CorrectingState = CorrectingStates.NotFixing;
                ExecuteScript();
            }
        }

        private static async void CheckForRuntimeErrors()
        {
            if (!string.IsNullOrEmpty(ErrorLogs))
                await CorrectScript();
            else
            {
                CorrectingState = CorrectingStates.NotFixing;
            }
        }

        public static async Task CorrectScript()
        {
            if (IsCorrectingScript)
                return;

            IsCorrectingScript = true;

            LLMInput.messages.Add(new Message
            {
                role = Role.user.ToString(),
                content = CorrectScriptPrompt + "\n" +
                        "Here's Unity's Console Error Logs :\n" +
                        ErrorLogs
            });

            ErrorLogs = null;
            UAAWindow.GeneratedString = "Correcting...";
            await CreateScript(true);
        }

        public static void ClearLog()
        {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }

        public static void UnsubscribeFromEvents()
        {
            Application.logMessageReceived -= SaveLogMessages;
        }
    }
}