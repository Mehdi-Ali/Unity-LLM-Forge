using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UAA
{
    [CreateAssetMenu(fileName = "UAASettings", menuName = "ScriptableObjects/UAA/Settings", order = 1)]
    public class UAASettingsSO : ScriptableObject
    {
        [Header("LLM API Settings")]
        public string LocalURL = "http://localhost:1234/v1/chat/completions";
        public string OpenAIURL = "https://api.openai.com/v1/chat/completions";
        public string OpenAI_API_Key = UAAProtectedEnv.OPENAI_API_KEY;
        public string OpenAI_API_model = "gpt-4-1106-preview";
        public bool LocalLLM = true;

        [Header("LLM Parameters")]
        public float Temperature = 0.5f;
        public int MaxTokens = -1;
        public bool Stream = true;

        [Header("Quality of Life Settings")]
        public bool SaveOnNewChat = false;
        public bool SaveOnLoad = false;
        public bool CallOnAwake = false;
        public bool SaveOnClose = false;
        public Color GUIBackgroundColor = new(0.2705882f, 0.3452933f, 0.3607843f, 1f);

        [Header("Paths")]
        public string ChatHistoryFolderPath = "Assets/UAA/ChatHistory";
        public string TempFilePath = "Assets/UAA/Commands/UAAGeneratedScript_temp.cs";
        public string SettingsPath = "Assets/UAA/Settings/UAASettings.asset";

        [Header("Runtime / cache")]
        public bool IsLLMAvailable = true;
        public CorrectingStates CorrectingState = CorrectingStates.NotFixing;
        public List<Message> CachedChatHistory = new List<Message>();
        public LocalLLMRequestInput CachedLLMInput;

        [Header("Prompts")]
        [SerializeField, TextArea(3, 10)] private string _defaultSystemMessage = UAADefaultPrompts.DefaultSystemMessage;
        public string SystemMessage
        {
            get { return string.IsNullOrEmpty(_defaultSystemMessage) ? UAADefaultPrompts.DefaultSystemMessage : _defaultSystemMessage; }
            set { _defaultSystemMessage = value; }
        }

        [SerializeField, TextArea(1, 3)] private string _defaultUserChatMessage = UAADefaultPrompts.DefaultUserChatMessage;
        public string UserChatMessage
        {
            get => _defaultUserChatMessage;
            set => _defaultUserChatMessage = value;
        }

        [SerializeField, TextArea(1, 3)] private string _defaultUserCommandMessage = UAADefaultPrompts.DefaultUserCommandMessage;
        public string UserCommandMessage
        {
            get => _defaultUserCommandMessage;
            set => _defaultUserCommandMessage = value;
        }

        [SerializeField, TextArea(3, 10)] private string _titlePrompt = UAADefaultPrompts.DefaultTitlePrompt;
        public string TitlePrompt
        {
            get { return string.IsNullOrEmpty(_titlePrompt) ? UAADefaultPrompts.DefaultTitlePrompt : _titlePrompt; }
            set { _titlePrompt = value; }
        }

        [SerializeField, TextArea(3, 10)] private string _simplifyCommandToTasksPrompt = UAADefaultPrompts.DefaultSimplifyCommandToTasksPrompt;
        public string SimplifyCommandToTasksPrompt
        {
            get { return string.IsNullOrEmpty(_simplifyCommandToTasksPrompt) ? UAADefaultPrompts.DefaultSimplifyCommandToTasksPrompt : _simplifyCommandToTasksPrompt; }
            set { _simplifyCommandToTasksPrompt = value; }
        }

        [SerializeField, TextArea(3, 10)] private string _taskToScriptPrompt = UAADefaultPrompts.DefaultTaskToScriptPrompt;	
        public string TaskToScriptPrompt
        {
            get { return string.IsNullOrEmpty(_taskToScriptPrompt) ? UAADefaultPrompts.DefaultTaskToScriptPrompt : _taskToScriptPrompt; }
            set { _taskToScriptPrompt = value; }
        }

        [SerializeField, TextArea(3, 3)] private string _correctScriptPrompt = UAADefaultPrompts.DefaultCorrectScriptPrompt;
        public string CorrectScriptPrompt
        {
            get { return string.IsNullOrEmpty(_correctScriptPrompt) ? UAADefaultPrompts.DefaultCorrectScriptPrompt : _correctScriptPrompt; }
            set { _correctScriptPrompt = value; }
        }
    }

    [Serializable]
    public enum CorrectingStates
    {
        NotFixing,
        FixingIDErrors,
        FixingRuntimeErrors
    }

    [Serializable]
    public enum OpenAIModels
    {
        GPT_4_Turbo,
        GPT_4,
        GPT_3_5_Turbo,
    }
}