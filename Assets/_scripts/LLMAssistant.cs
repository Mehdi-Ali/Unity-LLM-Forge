using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using EasyButtons;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.SocialPlatforms;

public class LLMAssistant : EditorWindow
{
    public static ToolLifecycleManagerSO LifecycleManager;

    public static string LocalURL { get => LifecycleManager.LocalURL; set => LifecycleManager.LocalURL = value; }
    public static string OpenAiURL { get => LifecycleManager.OpenAiURL; set => LifecycleManager.OpenAiURL = value; }
    public static string OpenAI_API_Key { get => LifecycleManager.OpenAI_API_Key; set => LifecycleManager.OpenAI_API_Key = value; }
    public static string OpenAI_API_model { get => LifecycleManager.OpenAI_API_model; set => LifecycleManager.OpenAI_API_model = value; }
    public static bool LocalLLM { get => LifecycleManager.LocalLLM; set => LifecycleManager.LocalLLM = value; }


    public static float Temperature { get => LifecycleManager.Temperature; set => LifecycleManager.Temperature = value; }
    public static int MaxTokens { get => LifecycleManager.MaxTokens; set => LifecycleManager.MaxTokens = value; }
    public static bool Stream { get => LifecycleManager.Stream; set => LifecycleManager.Stream = value; }

    // go back to bing here 
    /*
    I need to understand something specific:
    i want to have a text field that will be filled with a defauled text the first time the user uses the tool but once they edit that text i want it to be saved in my ToolLifecycleManagerSO so that they don't have to enter it everytime, and if the text is completly deleted it will be filll back with the default text
     */
    private string _systemMessage = Prompts.SystemMessage;
    private string _defaultUserChatMessage = Prompts.DefaultUserChatMessage;
    private string _defaultUserCommandMessage = Prompts.DefaultUserCommandMessage;

    private List<Message> _chatHistory { get => LifecycleManager.CachedChatHistory; set => LifecycleManager.CachedChatHistory = value; }

    private bool _isLLMAvailable { get => LifecycleManager.IsLLMAvailable; set => LifecycleManager.IsLLMAvailable = value; }

    public static string ChatHistoryFolderPath { get => LifecycleManager.ChatHistoryFolderPath; set => LifecycleManager.ChatHistoryFolderPath = value; }
    private SavedChatHistorySO _savedChatHistory;

    public static string GeneratedString = "";
    private string _userChatMessage;
    private string _userCommandMessage;

    // FrontEnd // Window stuff
    private bool SaveOnNewChat { get => LifecycleManager.SaveOnNewChat; set => LifecycleManager.SaveOnNewChat = value; }
    private bool SaveOnLoad { get => LifecycleManager.SaveOnLoad; set => LifecycleManager.SaveOnLoad = value; }

    bool _callOnAwake { get => LifecycleManager.CallOnAwake; set => LifecycleManager.CallOnAwake = value; }
    bool _saveOnClose { get => LifecycleManager.SaveOnClose; set => LifecycleManager.SaveOnClose = value;}

    private Vector2 _scrollPositionSystemMessage;
    private Vector2 _scrollPositionUserChatMessage;
    private Vector2 _scrollPositionUserCommandMessage;
    private Vector2 _scrollPositionChatHistory;
    private Vector2 _scrollPositionGeneratedScript;

    GUIStyle _roleStyle;
    GUIStyle _messageStyle;

    private int _selectedTab = 0;
    private Color _chatHistoryColor;

    private string[] savedChatHistoryPaths;
    private int selectedChatHistoryIndex;


    //! Should only handle Only Frontend and extract the LLMChatBot logic to another script
    void OnEnable()
    {
        LifecycleManager = AssetDatabase.LoadAssetAtPath<ToolLifecycleManagerSO>("Assets/UnityLMForge/ToolLifecycleManager.asset");

        _chatHistoryColor = new(0.1f, 0.1f, 0.1f);

        RefreshChatHistory();
        if (_callOnAwake)
            _ = InitializeNewChat();

        _userChatMessage = _defaultUserChatMessage;
        _userCommandMessage = _defaultUserCommandMessage;
    }

    [MenuItem("UnityLLMForge/LLMChatBot")]
    public static void ShowWindow()
    {
        GetWindow<LLMAssistant>("LLMChatBot");
    }

    public void OnInspectorUpdate()
    {
        Repaint();
    }

    private void OnGUI()
    {
        _roleStyle = new GUIStyle(GUI.skin.label);
        _messageStyle = new GUIStyle(GUI.skin.label);

        _messageStyle.normal.textColor = Color.white;
        _roleStyle.normal.textColor = Color.white;

        _roleStyle.wordWrap = true;
        _messageStyle.wordWrap = true;

        GUI.backgroundColor = Color.gray;

        var onEnter = (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return && !Event.current.shift);


        string[] tabs = { "Assistant Profile", "Assistant Chat", "Assistant Command" };
        _selectedTab = GUILayout.Toolbar(_selectedTab, tabs);

        switch (_selectedTab)
        {
            case 0:
                EditorGUILayout.LabelField("Profile Settings", EditorStyles.boldLabel);

                Stream = EditorGUILayout.Toggle("Stream", Stream);
                LocalLLM = EditorGUILayout.Toggle("Using Local LLM", LocalLLM);

                if (LocalLLM)
                {
                    LocalURL = EditorGUILayout.TextField("Local URL", LocalURL);
                    Temperature = EditorGUILayout.Slider("Creativity", Temperature, 0, 1);
                    MaxTokens = EditorGUILayout.IntField("Max Tokens", MaxTokens);
                }
                else
                {
                    OpenAiURL = EditorGUILayout.TextField("Open AI URL", OpenAiURL);
                    OpenAI_API_Key = EditorGUILayout.TextField("Open AI API KEY", OpenAI_API_Key);
                    OpenAI_API_model = EditorGUILayout.TextField("Open AI Used Model", OpenAI_API_model);
                }

                EditorGUILayout.LabelField("Profile Description");
                _scrollPositionSystemMessage = EditorGUILayout.BeginScrollView(_scrollPositionSystemMessage, GUILayout.Height(300));
                _systemMessage = GUILayout.TextArea(_systemMessage, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
                break;
            case 1:

                EditorGUILayout.LabelField("Chat Options", EditorStyles.boldLabel);
                if (GUILayout.Button("Initialize New Chat"))
                {
                    _ = InitializeNewChat();
                }
                SaveOnNewChat = EditorGUILayout.Toggle("Save current chat OnNew Chat", SaveOnNewChat);

                if (GUILayout.Button("Save Chat History"))
                {
                    SaveChatHistory(_chatHistory);
                }

                string[] savedChatHistoryNames = savedChatHistoryPaths.Select(path => Path.GetFileNameWithoutExtension(path)).ToArray();
                selectedChatHistoryIndex = EditorGUILayout.Popup("Chat History", selectedChatHistoryIndex, savedChatHistoryNames);

                if (selectedChatHistoryIndex <= savedChatHistoryPaths.Length - 1)
                {
                    if (selectedChatHistoryIndex == -1)
                        _savedChatHistory = null;
                    else
                        _savedChatHistory = AssetDatabase.LoadAssetAtPath<SavedChatHistorySO>(savedChatHistoryPaths[selectedChatHistoryIndex]);
                }

                SaveOnLoad = EditorGUILayout.Toggle("Save current chat OnLoad ", SaveOnLoad);

                // if (GUILayout.Button("Refresh Chat History List"))
                // {
                //     RefreshChatHistory();
                // }

                if (GUILayout.Button("Load Chat History"))
                {
                    LoadChatHistory();
                }

                EditorGUILayout.LabelField("Chat History", EditorStyles.boldLabel);
                _scrollPositionChatHistory = EditorGUILayout.BeginScrollView(_scrollPositionChatHistory, GUILayout.Height(500));

                for (int i = 1; i < _chatHistory.Count; i++)
                {
                    Message message = _chatHistory[i];

                    if (message.role == "user" && message.content == _defaultUserChatMessage)
                        continue;


                    string messageRole;
                    if (i == 1)
                        messageRole = message.role + ": ";
                    else
                        messageRole = "\n" + message.role + ": ";


                    _roleStyle.normal.textColor = message.role == "user" ? Color.magenta : Color.cyan;
                    GUIContent roleContent = new GUIContent(messageRole);
                    float roleHeight = _roleStyle.CalcHeight(roleContent, EditorGUIUtility.currentViewWidth);
                    Rect roleRect = GUILayoutUtility.GetRect(roleContent, _roleStyle, GUILayout.Height(roleHeight));
                    EditorGUI.DrawRect(roleRect, _chatHistoryColor);
                    EditorGUI.SelectableLabel(roleRect, messageRole, _roleStyle);

                    GUIContent contentContent = new GUIContent(message.content);
                    float contentHeight = _messageStyle.CalcHeight(contentContent, EditorGUIUtility.currentViewWidth);
                    Rect contentRect = GUILayoutUtility.GetRect(contentContent, _messageStyle, GUILayout.Height(contentHeight));
                    Rect backgroundRect = new Rect(contentRect.x, contentRect.y, contentRect.width, contentHeight);
                    EditorGUI.DrawRect(backgroundRect, _chatHistoryColor);
                    contentRect = new Rect(contentRect.x + 10, contentRect.y, contentRect.width - 20, contentHeight);
                    EditorGUI.SelectableLabel(contentRect, message.content, _messageStyle);
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.LabelField("Message The Assistant...", EditorStyles.boldLabel);
                _scrollPositionUserChatMessage = EditorGUILayout.BeginScrollView(_scrollPositionUserChatMessage, GUILayout.Height(100));
                _userChatMessage = GUILayout.TextArea(_userChatMessage, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Send Message") || onEnter)
                {
                    if (onEnter)
                        _userChatMessage = _userChatMessage.TrimEnd('\n');

                    SendMessage();
                    _scrollPositionChatHistory.y = Mathf.Infinity;
                    Repaint();
                }
                if (GUILayout.Button("Stop Generating") || onEnter)
                {
                    StopGenerating();
                }

                break;
            case 2:
                EditorGUILayout.LabelField("Command The Assistant...", EditorStyles.boldLabel);
                _scrollPositionUserCommandMessage = EditorGUILayout.BeginScrollView(_scrollPositionUserCommandMessage, GUILayout.Height(100));
                _userCommandMessage = GUILayout.TextArea(_userCommandMessage, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Send Command") || onEnter)
                {
                    if (onEnter)
                        _userCommandMessage = _userCommandMessage.TrimEnd('\n');

                    SendCommand();
                    Repaint();
                }

                EditorGUILayout.LabelField("Current Task's Script...", EditorStyles.boldLabel);
                _scrollPositionGeneratedScript = EditorGUILayout.BeginScrollView(_scrollPositionGeneratedScript, GUILayout.Height(600));
                GeneratedString = GUILayout.TextArea(GeneratedString, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                EditorGUILayout.LabelField("Validate Step...", EditorStyles.boldLabel);

                if (GUILayout.Button("Execute Script"))
                    AssistantCommand.ExecuteScript();

                if (GUILayout.Button("Ask Assistant to correct Script"))
                    AssistantCommand.CorrectScript();

                if (GUILayout.Button("ClearLogs"))
                    AssistantCommand.ClearLog();

                if (GUILayout.Button("Delete Generated Script"))
                    AssistantCommand.DeleteGeneratedScript();

                if (GUILayout.Button("Save Command History"))
                    SaveChatHistory(AssistantCommand.LLMInput.messages, false);
                if (GUILayout.Button("Log command History"))
                    LogMessages(AssistantCommand.LLMInput.messages);

                break;
        }
    }

    private void RefreshChatHistory()
    {
        string[] guids = AssetDatabase.FindAssets("t:SavedChatHistorySO", new[] { ChatHistoryFolderPath });
        savedChatHistoryPaths = new string[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            savedChatHistoryPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
        }
    }

    private void LoadChatHistory()
    {
        RefreshChatHistory();

        if (_savedChatHistory == null)
        {
            Debug.LogError("Invalid chat history selected");
            return;
        }

        if (SaveOnLoad == true)
        {
            var index = 0;
            if (selectedChatHistoryIndex <= savedChatHistoryPaths.Length - 1)
                index = selectedChatHistoryIndex;

            SaveChatHistory(_chatHistory, false);
        }

        _chatHistory = new List<Message>(_savedChatHistory.ChatHistory);
    }

    private async Task InitializeNewChat()
    {
        selectedChatHistoryIndex = -1;

        if (SaveOnNewChat == true)
            SaveChatHistory(_chatHistory, false);

        while (!_isLLMAvailable)
        {
            await Task.Delay(100);
        }

        _chatHistory.Clear();
        _chatHistory.Add(new Message { role = "system", content = _systemMessage });

        if (String.IsNullOrEmpty(_userChatMessage))
            _userChatMessage = _defaultUserChatMessage;

        _chatHistory.Add(new Message { role = "user", content = _userChatMessage });
        _userChatMessage = "";
        _ = LLMChat();

        await Task.Delay(1);
    }

    private void SaveChatHistory(List<Message> messages, bool setIndex = true)
    {
        LogMessages(messages);
        if (messages.Count <= 0)
            return;

        var saveSlot = ScriptableObject.CreateInstance<SavedChatHistorySO>();
        saveSlot.ChatHistory = new(messages);
        string dateTime = DateTime.Now.ToString("yy-MM-dd HH-mm");

        string assetPath = ChatHistoryFolderPath + $"/{dateTime} Chat History.asset";
        AssetDatabase.CreateAsset(saveSlot, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        if (setIndex)
            selectedChatHistoryIndex = savedChatHistoryPaths.Length;

        _ = RenameChatHistory(dateTime, assetPath);

    }

    private async Task RenameChatHistory(string dateTime, string assetPath)
    {

        _chatHistory.Add(new Message { role = "user", content = Prompts.TitlePrompt });
        await LLMChat();

        string chatHistoryName = CleanAssetName(_chatHistory[_chatHistory.Count - 1].content);

        _chatHistory.RemoveAt(_chatHistory.Count - 1);
        _chatHistory.RemoveAt(_chatHistory.Count - 1);
        AssetDatabase.RenameAsset(assetPath, $"{dateTime} {chatHistoryName}.asset");

        RefreshChatHistory();
    }

    private string CleanAssetName(string chatHistoryName)
    {
        if (chatHistoryName.StartsWith("Title", StringComparison.OrdinalIgnoreCase))
        {
            chatHistoryName = chatHistoryName.Substring(5).TrimStart();
        }

        chatHistoryName = Regex.Replace(chatHistoryName, "[^a-zA-Z0-9 -]", ""); ;
        return chatHistoryName;
    }

    public void SendMessage()
    {
        if (_isLLMAvailable == false)
        {
            Debug.Log("LLM is not available");
            return;
        }

        _chatHistory.Add(new Message { role = "user", content = _userChatMessage });
        _userChatMessage = "";
        _ = LLMChat();
    }

    public async Task LLMChat()
    {
        _isLLMAvailable = false;

        var llmInput = new LocalLLMInput
        {
            messages = _chatHistory,
            temperature = Temperature,
            max_tokens = MaxTokens,
            stream = Stream
        };

        if (Stream)
        {
            await LLMConnection.SendAndReceiveStreamedMessages(llmInput, messageContent =>
            {
                if (_chatHistory.Last().role == "assistant")
                    _chatHistory.RemoveAt(_chatHistory.Count - 1);

                _chatHistory.Add(new Message { role = "assistant", content = messageContent });
                Repaint();
            });
        }

        else
        {
            _chatHistory.Add(new Message { role = "assistant", content = "Crafting a response…" });
            string messageContent = await LLMConnection.SendAndReceiveNonStreamedMessages(llmInput);
            _chatHistory.RemoveAt(_chatHistory.Count - 1);
            _chatHistory.Add(new Message { role = "assistant", content = messageContent });
            Repaint();
        }

        _scrollPositionChatHistory.y = Mathf.Infinity;
        _isLLMAvailable = true;
    }

    public void SendCommand()
    {
        if (_isLLMAvailable == false)
        {
            Debug.Log("LLM is not available");
            return;
        }

        AssistantCommand.InitializeCommand(_userCommandMessage);
    }

    public void StopGenerating()
    {
        _isLLMAvailable = true;
        LLMConnection.StopGenerating();
    }


    private static void LogMessages(List<Message> messages)
    {
        foreach (var message in messages)
        {
            Debug.Log(message.role + ": " + message.content);
        }
    }

    void OnDestroy()
    {
        if (_saveOnClose)
            SaveChatHistory(_chatHistory);

        AssistantCommand.UnsubscribeFromEvents();
    }
}



