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


namespace UAA
{
    public class UAAWindow : EditorWindow
    {
        public static UAASettingsSO Settings;

        public static string LocalURL { get => Settings.LocalURL; set => Settings.LocalURL = value; }
        public static string OpenAiURL { get => Settings.OpenAiURL; set => Settings.OpenAiURL = value; }
        public static string OpenAI_API_Key { get => Settings.OpenAI_API_Key; set => Settings.OpenAI_API_Key = value; }
        public static string OpenAI_API_model { get => Settings.OpenAI_API_model; set => Settings.OpenAI_API_model = value; }
        public static bool LocalLLM { get => Settings.LocalLLM; set => Settings.LocalLLM = value; }

        public static float Temperature { get => Settings.Temperature; set => Settings.Temperature = value; }
        public static int MaxTokens { get => Settings.MaxTokens; set => Settings.MaxTokens = value; }
        public static bool Stream { get => Settings.Stream; set => Settings.Stream = value; }

        public static string SystemMessage { get => Settings.SystemMessage; set => Settings.SystemMessage = value; }
        public static string UserChatMessage { get => Settings.DefaultUserChatMessage; set => Settings.DefaultUserChatMessage = value; }
        private string UserCommandMessage { get => Settings.DefaultUserCommandMessage; set => Settings.DefaultUserCommandMessage = value; }

        public static bool IsLLMAvailable { get => Settings.IsLLMAvailable; set => Settings.IsLLMAvailable = value; }

        public static List<Message> ChatHistory { get => Settings.CachedChatHistory; set => Settings.CachedChatHistory = value; }

        public static string GeneratedString = "";
        public  static UAAChatHistorySO SavedChatHistory;

        #region FrontEnd --------------------------------------------------------------------------------------------------------------------------------
        public static bool SaveOnNewChat { get => Settings.SaveOnNewChat; set => Settings.SaveOnNewChat = value; }
        public static bool SaveOnLoad { get => Settings.SaveOnLoad; set => Settings.SaveOnLoad = value; }

        bool _callOnAwake { get => Settings.CallOnAwake; set => Settings.CallOnAwake = value; }
        bool _saveOnClose { get => Settings.SaveOnClose; set => Settings.SaveOnClose = value; }

        private static bool _onEnter;

        public static string[] SavedChatHistoryPaths;

        private static Vector2 _scrollPositionSystemMessage;
        private static Vector2 _scrollPositionUserChatMessage;
        private static Vector2 _scrollPositionUserCommandMessage;
        private static Vector2 _scrollPositionChatHistory;
        private static Vector2 _scrollPositionGeneratedScript;

        private static float totalHeight;

        GUIStyle _roleStyle;
        GUIStyle _messageStyle;

        private int _selectedTab = 0;
        private Color ChatHistoryColor;

        public static int selectedChatHistoryIndex;
       

        [MenuItem("Window/UAA - Unity AI Assistant")]
        public static void ShowWindow()
        {
            GetWindow<UAAWindow>("UAA");
        }

        private void OnGUI()
        {
            InitializeStyles();
            GUI.backgroundColor = Color.gray;
            _onEnter = (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return && !Event.current.shift);

            string[] tabs = { "UAA Profile", "UAA Chat", "UAA Command" };
            _selectedTab = GUILayout.Toolbar(_selectedTab, tabs);

            switch (_selectedTab)
            {
                case 0:
                    ProfileTab();
                    break;
                case 1:
                    ChatTab();
                    break;
                case 2:
                    CommandTab();
                    break;
            }
        }

        private void InitializeStyles()
        {
            _roleStyle = new GUIStyle(GUI.skin.label);
            _messageStyle = new GUIStyle(GUI.skin.label);

            _messageStyle.normal.textColor = Color.white;
            _roleStyle.normal.textColor = Color.white;

            _roleStyle.wordWrap = true;
            _messageStyle.wordWrap = true;
        }

        private void ProfileTab()
        {
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
            SystemMessage = GUILayout.TextArea(SystemMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void ChatTab()
        {
            EditorGUILayout.LabelField("Chat Options", EditorStyles.boldLabel);
            if (GUILayout.Button("Initialize New Chat"))
            {
                _ = UAAChat.InitializeNewChat();
            }
            SaveOnNewChat = EditorGUILayout.Toggle("Save current chat OnNew Chat", SaveOnNewChat);

            if (GUILayout.Button("Save Chat History"))
            {
                UAAChat.SaveChatHistory(ChatHistory);
            }

            string[] savedChatHistoryNames = SavedChatHistoryPaths.Select(path => Path.GetFileNameWithoutExtension(path)).ToArray();
            selectedChatHistoryIndex = EditorGUILayout.Popup("Chat History", selectedChatHistoryIndex, savedChatHistoryNames);

            if (selectedChatHistoryIndex <= SavedChatHistoryPaths.Length - 1)
            {
                if (selectedChatHistoryIndex == -1)
                    SavedChatHistory = null;
                else
                    SavedChatHistory = AssetDatabase.LoadAssetAtPath<UAAChatHistorySO>(SavedChatHistoryPaths[selectedChatHistoryIndex]);
            }

            SaveOnLoad = EditorGUILayout.Toggle("Save current chat OnLoad ", SaveOnLoad);

            if (GUILayout.Button("Refresh Chat History List"))
            {
                UAAChat.RefreshChatHistory();
            }

            if (GUILayout.Button("Load Chat History"))
            {
                UAAChat.LoadChatHistory();
            }

            EditorGUILayout.LabelField("Chat History", EditorStyles.boldLabel);
            _scrollPositionChatHistory = EditorGUILayout.BeginScrollView(_scrollPositionChatHistory, GUILayout.Height(500));

            totalHeight = -475f;

            for (int i = 1; i < ChatHistory.Count; i++)
            {
                Message message = ChatHistory[i];

                string messageRole;
                if (i == 1)
                    messageRole = message.role + ": ";
                else
                    messageRole = "\n" + message.role + ": ";

                _roleStyle.normal.textColor = message.role == "user" ? Color.magenta : Color.cyan;
                GUIContent roleContent = new GUIContent(messageRole);
                float roleHeight = _roleStyle.CalcHeight(roleContent, EditorGUIUtility.currentViewWidth);
                Rect roleRect = GUILayoutUtility.GetRect(roleContent, _roleStyle, GUILayout.Height(roleHeight));
                EditorGUI.DrawRect(roleRect, ChatHistoryColor);
                EditorGUI.SelectableLabel(roleRect, messageRole, _roleStyle);

                GUIContent contentContent = new GUIContent(message.content);
                float contentHeight = _messageStyle.CalcHeight(contentContent, EditorGUIUtility.currentViewWidth);
                Rect contentRect = GUILayoutUtility.GetRect(contentContent, _messageStyle, GUILayout.Height(contentHeight));
                Rect backgroundRect = new Rect(contentRect.x, contentRect.y, contentRect.width, contentHeight);
                EditorGUI.DrawRect(backgroundRect, ChatHistoryColor);
                contentRect = new Rect(contentRect.x + 10, contentRect.y, contentRect.width - 20, contentHeight);
                EditorGUI.SelectableLabel(contentRect, message.content, _messageStyle);

                totalHeight += roleHeight + contentHeight;
            }

            EditorGUILayout.EndScrollView();


            EditorGUILayout.LabelField("Message The Assistant...", EditorStyles.boldLabel);
            _scrollPositionUserChatMessage = EditorGUILayout.BeginScrollView(_scrollPositionUserChatMessage, GUILayout.Height(100));
            UserChatMessage = GUILayout.TextArea(UserChatMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Send Message") || _onEnter)
            {
                if (_onEnter)
                    UserChatMessage = UserChatMessage.TrimEnd('\n');

                SendMessage();
                _scrollPositionChatHistory.y = Mathf.Infinity;
                Repaint();
            }
            if (GUILayout.Button("Stop Generating") || _onEnter)
            {
                StopGenerating();
            }
        }

        private void CommandTab()
        {
            EditorGUILayout.LabelField("Command The Assistant...", EditorStyles.boldLabel);
            _scrollPositionUserCommandMessage = EditorGUILayout.BeginScrollView(_scrollPositionUserCommandMessage, GUILayout.Height(100));
            UserCommandMessage = GUILayout.TextArea(UserCommandMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Send Command") || _onEnter)
            {
                if (_onEnter)
                    UserCommandMessage = UserCommandMessage.TrimEnd('\n');

                SendCommand();
                Repaint();
            }

            EditorGUILayout.LabelField("Current Task's Script...", EditorStyles.boldLabel);
            _scrollPositionGeneratedScript = EditorGUILayout.BeginScrollView(_scrollPositionGeneratedScript, GUILayout.Height(600));
            GeneratedString = GUILayout.TextArea(GeneratedString, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Validate Step...", EditorStyles.boldLabel);

            if (GUILayout.Button("Execute Script"))
                UAACommand.ExecuteScript();

            if (GUILayout.Button("Ask Assistant to correct Script"))
                UAACommand.CorrectScript();

            if (GUILayout.Button("ClearLogs"))
                UAACommand.ClearLog();

            if (GUILayout.Button("Delete Generated Script"))
                UAACommand.DeleteGeneratedScript();

            if (GUILayout.Button("Save Command History"))
                UAAChat.SaveChatHistory(UAACommand.LLMInput.messages, false);
            if (GUILayout.Button("Log command History"))
                LogMessages(UAACommand.LLMInput.messages);
        }

        public void OnInspectorUpdate()
        {
            Repaint();
        }
        #endregion --------------------------------------------------------------------------------------------------------------------------------

        void OnEnable()
        {
            Settings = AssetDatabase.LoadAssetAtPath<UAASettingsSO>("Assets/UAA/Settings/UAASettings.asset");

            if (Settings == null)
            {
                Settings = CreateInstance<UAASettingsSO>();
                AssetDatabase.CreateAsset(Settings, "Assets/UAA/Settings/UAASettings.asset");
            }

            ChatHistoryColor = new(0.1f, 0.1f, 0.1f);

            UAAChat.RefreshChatHistory();
            if (_callOnAwake)
                _ = UAAChat.InitializeNewChat();
        }



        public void SendMessage()
        {
            if (IsLLMAvailable == false)
            {
                Debug.Log("LLM is not available");
                return;
            }

            ChatHistory.Add(new Message { role = "user", content = UserChatMessage });
            UserChatMessage = "";
            _ = LLMChat();
        }

        public static async Task LLMChat()
        {
            IsLLMAvailable = false;

            var llmInput = new LocalLLMInput
            {
                messages = ChatHistory,
                temperature = Temperature,
                max_tokens = MaxTokens,
                stream = Stream
            };

            if (Stream)
            {
                await UAAConnection.SendAndReceiveStreamedMessages(llmInput, messageContent =>
                {
                    if (ChatHistory.Last().role == "assistant")
                        ChatHistory.RemoveAt(ChatHistory.Count - 1);

                    ChatHistory.Add(new Message { role = "assistant", content = messageContent });

                    if (_scrollPositionChatHistory.y > totalHeight * 0.75)
                        _scrollPositionChatHistory.y = Mathf.Infinity;
                });
            }

            else
            {
                ChatHistory.Add(new Message { role = "assistant", content = "Crafting a response…" });
                string messageContent = await UAAConnection.SendAndReceiveNonStreamedMessages(llmInput);
                ChatHistory.RemoveAt(ChatHistory.Count - 1);
                ChatHistory.Add(new Message { role = "assistant", content = messageContent });
            }

            _scrollPositionChatHistory.y = Mathf.Infinity;
            IsLLMAvailable = true;
        }

        public void SendCommand()
        {
            if (IsLLMAvailable == false)
            {
                Debug.Log("LLM is not available");
                return;
            }

            UAACommand.InitializeCommand(UserCommandMessage);
        }

        public void StopGenerating()
        {
            IsLLMAvailable = true;
            UAAConnection.StopGenerating();
        }

        private static void LogMessages(List<Message> messages)
        {
            foreach (var message in messages)
            {
                Debug.Log(message.role + ": " + message.content);
            }
        }
    }
}



