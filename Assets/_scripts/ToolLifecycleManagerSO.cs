using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ToolLifecycleManager", menuName = "ScriptableObjects/UnityLMForge/ToolLifecycleManager", order = 1)]
public class ToolLifecycleManagerSO : ScriptableObject
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

    [Header("Run Time")]
    public bool IsLLMAvailable = true;



    [Header("Paths")]
    public string ChatHistoryFolderPath = $"Assets/UnityLMForge/ChatHistory";

    [Header("cache")]
    public List<Message> CachedChatHistory = new List<Message>();
    public LocalLLMInput CachedLLMInput;


    [Header("Messages")]
    public string SystemMessage = Prompts.SystemMessage;
    public string DefaultUserChatMessage = Prompts.DefaultUserChatMessage;
    public string DefaultUserCommandMessage = Prompts.DefaultUserCommandMessage;
}