using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    [Header("Messages")]
    public string SystemMessage = UAAPrompts.SystemMessage;
    public string DefaultUserChatMessage = UAAPrompts.DefaultUserChatMessage;
    public string DefaultUserCommandMessage = UAAPrompts.DefaultUserCommandMessage;
}

[Serializable]
public enum CorrectingStates
{
    NotFixing,
    FixingIDErrors,
    FixingRuntimeErrors
}