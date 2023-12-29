using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UAA
{
    [CreateAssetMenu(fileName = "UAASettings", menuName = "ScriptableObjects/UnityLMForge/UAASettings", order = 1)]
    public class UAASettingsSO : ScriptableObject
    {
        [Header("LLM API Settings")]
        public string LocalURL = "http://localhost:1234/v1/chat/completions";
        public string OpenAiURL = "https://api.openai.com/v1/chat/completions";
        public string OpenAI_API_Key = "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";
        public string OpenAI_API_model = "gpt-3.5-turbo";
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

        [Header("Paths")]
        public string ChatHistoryFolderPath = "Assets/UAA/ChatHistory";

        [Header("Runtime / cache")]
        public bool IsLLMAvailable = true;
        public CorrectingStates CorrectingState = CorrectingStates.NotFixing;
        public List<Message> CachedChatHistory = new List<Message>();
        public LocalLLMInput CachedLLMInput;


        [Header("Prompts")]
        [SerializeField, TextArea(3, 10)] private string _defaultSystemMessage = UAAPrompts.DefaultSystemMessage;
        public string SystemMessage
        {
            get { return string.IsNullOrEmpty(_defaultSystemMessage) ? UAAPrompts.DefaultSystemMessage : _defaultSystemMessage; }
            set { _defaultSystemMessage = value; }
        }

        [SerializeField, TextArea(1, 3)] private string _defaultUserChatMessage = UAAPrompts.DefaultUserChatMessage;
        public string DefaultUserChatMessage
        {
            get { return string.IsNullOrEmpty(_defaultUserChatMessage) ? UAAPrompts.DefaultUserChatMessage : _defaultUserChatMessage; }
            set { _defaultUserChatMessage = value; }
        }

        [SerializeField, TextArea(1, 3)] private string _defaultUserCommandMessage = UAAPrompts.DefaultUserCommandMessage;
        public string DefaultUserCommandMessage
        {
            get { return string.IsNullOrEmpty(_defaultUserCommandMessage) ? UAAPrompts.DefaultUserCommandMessage : _defaultUserCommandMessage; }
            set { _defaultUserCommandMessage = value; }
        }

        [SerializeField, TextArea(3, 10)] private string _titlePrompt = UAAPrompts.TitlePrompt;
        public string TitlePrompt
        {
            get { return string.IsNullOrEmpty(_titlePrompt) ? UAAPrompts.TitlePrompt : _titlePrompt; }
            set { _defaultUserCommandMessage = value; }
        }

        [SerializeField, TextArea(3, 10)] private string _simplifyCommandToTasksPrompt = UAAPrompts.SimplifyCommandToTasksPrompt;
        public string SimplifyCommandToTasksPrompt
        {
            get { return string.IsNullOrEmpty(_simplifyCommandToTasksPrompt) ? UAAPrompts.SimplifyCommandToTasksPrompt : _simplifyCommandToTasksPrompt; }
            set { _simplifyCommandToTasksPrompt = value; }
        }

        [SerializeField, TextArea(3, 10)] private string _taskToScriptPrompt = UAAPrompts.TaskToScriptPrompt;	
        public string TaskToScriptPrompt
        {
            get { return string.IsNullOrEmpty(_taskToScriptPrompt) ? UAAPrompts.TaskToScriptPrompt : _taskToScriptPrompt; }
            set { _taskToScriptPrompt = value; }
        }

        [SerializeField, TextArea(3, 3)] private string _correctScriptPrompt = UAAPrompts.CorrectScriptPrompt;
        public string CorrectScriptPrompt
        {
            get { return string.IsNullOrEmpty(_correctScriptPrompt) ? UAAPrompts.CorrectScriptPrompt : _correctScriptPrompt; }
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
}