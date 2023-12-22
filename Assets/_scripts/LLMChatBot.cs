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

public class LLMChatBot : EditorWindow
{
    string url = "http://localhost:1234/v1/chat/completions";
    private string _systemMessage = WorkflowPrompts.SystemMessage;
    private string _defaultUserMessage = WorkflowPrompts.DefaultUserMessage;
    float _temperature = 0.5f;
    int _maxTokens = -1;
    bool _stream = true;
    private List<Message> _chatHistory = new List<Message>();
    private string _assistantMessage;
    private string _userMessage;
    private SavedChatHistorySO _savedChatHistory;
    string _folderPath = $"Assets/UnityLMForge/ChatHistory";


    private bool _isLLMAvailable = true;

    // Window stuff
    private Vector2 _scrollPositionSystemMessage;
    private Vector2 _scrollPositionUserMessage;
    private Vector2 _scrollPositionChatHistory;

    GUIStyle _roleStyle;
    GUIStyle _messageStyle;
    private TextEditor _textEditor = new TextEditor();


    private int _selectedTab = 0;
    private Color _chatHistoryColor;


    bool _saveOnNewChat = false;
    bool _saveOnload = false;


    // cache the bool in user settings, or just create a scriptable object that hold the settings
    bool _callOnAwake = false;
    bool _saveOnClose = false;



    private string[] savedChatHistoryPaths;
    private int selectedChatHistoryIndex;



    void OnEnable()
    {
        _selectedTab = 1;
        _chatHistoryColor = new(0.1f, 0.1f, 0.1f);

        RefreshChatHistory();
        if (_callOnAwake)
            _ = InitializeNewChat();
    }

    [MenuItem("UnityLLMForge/LLMChatBot")] 
    public static void ShowWindow()
    {
        GetWindow<LLMChatBot>("LLMChatBot");
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



    
        string[] tabs = { "Assistant Profile", "Assistant Chat", "Assistant Command" };
        _selectedTab = GUILayout.Toolbar(_selectedTab, tabs);

        switch (_selectedTab)
        {
            case 0:
                EditorGUILayout.LabelField("Profile Settings", EditorStyles.boldLabel);
                url = EditorGUILayout.TextField("URL", url);
                _callOnAwake = EditorGUILayout.Toggle("Call On Awake", _callOnAwake);

                EditorGUILayout.LabelField("Profile Description");
                _scrollPositionSystemMessage = EditorGUILayout.BeginScrollView(_scrollPositionSystemMessage, GUILayout.Height(300)); 
                _systemMessage = GUILayout.TextArea(_systemMessage, GUILayout.ExpandHeight(true)); 
                EditorGUILayout.EndScrollView();

                _temperature = EditorGUILayout.Slider("Creativity", _temperature, 0, 1);

                _maxTokens = EditorGUILayout.IntField("Max Tokens", _maxTokens);
                break;
            case 1:

                EditorGUILayout.LabelField("Chat Options", EditorStyles.boldLabel);
                if (GUILayout.Button("Initialize New Chat"))
                {
                    _ = InitializeNewChat();
                }
                _saveOnNewChat = EditorGUILayout.Toggle("Save current chat OnNew Chat", _saveOnNewChat);

                if (GUILayout.Button("Save Chat History"))
                {
                    SaveChatHistory();
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

                _saveOnload = EditorGUILayout.Toggle("Save current chat OnLoad ", _saveOnload);

                if (GUILayout.Button("Refresh Chat History List"))
                {
                    RefreshChatHistory();
                }

                if (GUILayout.Button("Load Chat History"))
                {
                    LoadChatHistory();
                }

                EditorGUILayout.LabelField("Chat History", EditorStyles.boldLabel);
                _scrollPositionChatHistory = EditorGUILayout.BeginScrollView(_scrollPositionChatHistory, GUILayout.Height(500));

                for (int i = 1; i < _chatHistory.Count; i++)
                {
                    Message message = _chatHistory[i];

                    if (message.role == "user" && message.content == _defaultUserMessage)
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
                _scrollPositionUserMessage = EditorGUILayout.BeginScrollView(_scrollPositionUserMessage, GUILayout.Height(100));
                _userMessage = GUILayout.TextArea(_userMessage, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                var onEnter  = (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return);
                if (GUILayout.Button("Send Message") || onEnter)
                {
                    if (onEnter)
                        _userMessage = _userMessage.TrimEnd('\n');
                    
                    SendMessage();
                    _scrollPositionChatHistory.y = Mathf.Infinity;
                    Repaint();
                    // focus on the _userMessage text area
                    //...
                }
                if (GUILayout.Button("Stop Generating") || onEnter)
                {
                    StopGenerating();
                }
                _stream = EditorGUILayout.Toggle("Stream", _stream);

                break;
            case 2:
                EditorGUILayout.LabelField("Coming Soon...", EditorStyles.boldLabel);
                break;
        }
    }

    private void RefreshChatHistory()
    {
        string[] guids = AssetDatabase.FindAssets("t:SavedChatHistorySO", new[] { _folderPath });
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
        
        if (_saveOnload == true)
        {
            var index = 0;
            if (selectedChatHistoryIndex <= savedChatHistoryPaths.Length - 1)
                index = selectedChatHistoryIndex;
            
            SaveChatHistory(false);
        }
        
        _chatHistory = new List<Message>(_savedChatHistory.ChatHistory);
    }

    private async Task InitializeNewChat()
    {
        selectedChatHistoryIndex  = -1;

        if (_saveOnNewChat == true)
            SaveChatHistory(false);

        while (!_isLLMAvailable)
        {
            await Task.Delay(100);
        }

        _chatHistory.Clear();   
        _chatHistory.Add(new Message { role = "system", content = _systemMessage });

        if (String.IsNullOrEmpty(_userMessage))
            _userMessage = _defaultUserMessage;

        _chatHistory.Add(new Message { role = "user", content = _userMessage });
        _userMessage = "";
        _ = LLMChat();

        await Task.Delay(1);
    }

    private void SaveChatHistory(bool setIndex = true)
    {
        if (_chatHistory.Count <= 0)
            return;

        var saveSlot = ScriptableObject.CreateInstance<SavedChatHistorySO>();
        saveSlot.ChatHistory = new(_chatHistory);
        string dateTime = DateTime.Now.ToString("yy-MM-dd HH-mm");

        string assetPath = _folderPath + $"/{dateTime} Chat History.asset";
        AssetDatabase.CreateAsset(saveSlot, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        if (setIndex)
            selectedChatHistoryIndex = savedChatHistoryPaths.Length;
       
        _ = RenameChatHistory(dateTime, assetPath);

    }

    private async Task RenameChatHistory(string dateTime, string assetPath)
    {

        _chatHistory.Add(new Message { role = "user", content = WorkflowPrompts.TitlePrompt});
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
            return;
        
        _chatHistory.Add(new Message { role = "user", content = _userMessage });
        _userMessage = "";
        _ = LLMChat();
    }

    public async Task LLMChat()
    {
        _isLLMAvailable = false;

        var llmInput = new LLMInput
        {
            messages = _chatHistory.ToList(),
            temperature = _temperature,
            max_tokens = _maxTokens,
            stream = _stream
        };

        if (_stream)
        {
            await LLMConnection.SendAndReceiveStreamedMessages(llmInput, url, messageContent =>
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
            string messageContent = await LLMConnection.SendAndReceiveNonStreamedMessages(llmInput, url);
            _chatHistory.RemoveAt(_chatHistory.Count - 1);
            _chatHistory.Add(new Message { role = "assistant", content = messageContent });
            Repaint();
        }

        _scrollPositionChatHistory.y = Mathf.Infinity;
        _isLLMAvailable = true;
    }


    public void StopGenerating()
    {
        _isLLMAvailable = true;
        LLMConnection.StopGenerating();
    }

    void OnDestroy()
    {
        if (_saveOnClose)
            SaveChatHistory();
    }
}



