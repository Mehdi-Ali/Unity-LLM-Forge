using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Unity.Mathematics;


namespace UAA
{
    public class UAAWindow : EditorWindow
    {
        private static UAASettingsSO _settings;
        private static UAASettingsSO Settings
        {
            get
            {
                var settingsPath = "Assets/UAA/Settings/UAASettings.asset";
                if (_settings == null)
                    _settings = AssetDatabase.LoadAssetAtPath<UAASettingsSO>(settingsPath);

                if (_settings == null)
                {
                    _settings = CreateInstance<UAASettingsSO>();
                    AssetDatabase.CreateAsset(_settings, settingsPath);
                }

                return _settings;
            }
        }

        #region Logic Variables--------------------------------------------------------------------------------------------------------------------------------

        public static string LocalURL
        {
            get => Settings.LocalURL;
            set => Settings.LocalURL = value;
        }

        public static string OpenAIURL
        {
            get => Settings.OpenAIURL;
            set => Settings.OpenAIURL = value;
        }

        public static string OpenAI_API_Key
        {
            get => Settings.OpenAI_API_Key;
            set => Settings.OpenAI_API_Key = value;
        }

        public static string OpenAI_API_model
        {
            get => Settings.OpenAI_API_model;
            set => Settings.OpenAI_API_model = value;
        }

        public static bool LocalLLM
        {
            get => Settings.LocalLLM;
            set => Settings.LocalLLM = value;
        }

        public static float Temperature
        {
            get => Settings.Temperature;
            set => Settings.Temperature = value;
        }

        public static int MaxTokens
        {
            get => Settings.MaxTokens;
            set => Settings.MaxTokens = value;
        }

        public static bool Stream
        {
            get => Settings.Stream;
            set => Settings.Stream = value;
        }

        public static string SystemMessage
        {
            get => Settings.Prompts.SystemMessage;
            set => Settings.Prompts.SystemMessage = value;
        }

        public static string UserChatMessage
        {
            get => Settings.Prompts.UserChatMessage;
            set => Settings.Prompts.UserChatMessage = value;
        }

        public static string UserCommandMessage
        {
            get => Settings.Prompts.UserCommandMessage;
            set => Settings.Prompts.UserCommandMessage = value;
        }

        public static bool IsLLMAvailable
        {
            get
            {
                if (Settings.IsLLMAvailable == false)
                    Debug.Log("LLM is not available");

                return Settings.IsLLMAvailable;
            }

            set => Settings.IsLLMAvailable = value;
        }

        public static List<Message> ChatHistory
        {
            get => Settings.CachedChatHistory;
            set => Settings.CachedChatHistory = value;
        }

        public static List<Message> CommandHistory
        {
            get => Settings.CachedLLMInput.messages;
            set => Settings.CachedLLMInput.messages = value;
        }

        public static string GeneratedString = "";
        public static UAAChatHistorySO SavedChatHistory;

        public static string TitlePrompt
        {
            get => Settings.Prompts.TitlePrompt;
            set => Settings.Prompts.TitlePrompt = value;
        }

        public static string SimplifyCommandToTasksPrompt
        {
            get => Settings.Prompts.SimplifyCommandToTasksPrompt;
            set => Settings.Prompts.SimplifyCommandToTasksPrompt = value;
        }

        public static string TaskToScriptPrompt
        {
            get => Settings.Prompts.TaskToScriptPrompt;
            set => Settings.Prompts.TaskToScriptPrompt = value;
        }

        public static string CorrectScriptPrompt
        {
            get => Settings.Prompts.CorrectScriptPrompt;
            set => Settings.Prompts.CorrectScriptPrompt = value;
        }

        public static int MaxIterationsBeforeRestarting
        {
            get => Settings.MaxIterationsBeforeRestarting;
            set => Settings.MaxIterationsBeforeRestarting = value;
        }

        #endregion Logic Variables ----------------------------------------------------------------------------------------------------------------------------

        #region FrontEnd --------------------------------------------------------------------------------------------------------------------------------

        public static bool SaveOnNewChat
        {
            get => Settings.SaveOnNewChat;
            set => Settings.SaveOnNewChat = value;
        }

        public static bool SaveOnLoad
        {
            get => Settings.SaveOnLoad;
            set => Settings.SaveOnLoad = value;
        }

        private bool CallOnAwake
        {
            get => Settings.CallOnAwake;
            set => Settings.CallOnAwake = value;
        }

        private bool SaveOnClose
        {
            get => Settings.SaveOnClose;
            set => Settings.SaveOnClose = value;
        }

        private static int SelectedTab
        {
            get => Settings.SelectedTab;
            set => Settings.SelectedTab = value;
        }

        public static OpenAIModels SelectedOpenAIModel;
        private readonly Dictionary<OpenAIModels, string> _modelToString = new()
        {
            { OpenAIModels.GPT_4_Turbo, "gpt-4-1106-preview" },
            { OpenAIModels.GPT_4, "gpt-4" },
            { OpenAIModels.GPT_3_5_Turbo, "gpt-3.5-turbo-1106" },
        };

        private static bool _onEnter;

        public static string[] SavedChatHistoryPaths;

        private static Vector2 _scrollPositionGeneral;
        private static Vector2 _scrollPositionSystemMessage;
        private static Vector2 _scrollPositionUserChatMessage;
        private static Vector2 _scrollPositionUserCommandMessage;
        private static Vector2 _scrollPositionChatHistory;
        private static Vector2 _scrollPositionGeneratedScript;
        private static Vector2 _scrollPositionTitlePrompt;
        private static Vector2 _scrollPositionSimplifyCommandToTasksPrompt;
        private static Vector2 _scrollPositionTaskToScriptPrompt;
        private static Vector2 _scrollPositionCorrectScriptPrompt;
        private static readonly float _messagesPadding = 20f;
        private static float _totalHeight;

        GUIStyle _roleStyle;
        GUIStyle _messageStyle;

        private Color _backgroundColor;
        private Color ChatHistoryColor;

        public static int selectedChatHistoryIndex;

        [MenuItem("Window/UAA - Unity AI Assistant")]
        private static void ShowWindow()
        {
            GetWindow<UAAWindow>("UAA");
            SavedChatHistory = null;
            selectedChatHistoryIndex = -1;
        }

        private void OnGUI()
        {
            InitializeStyles();
            GUI.backgroundColor = _backgroundColor;
            _onEnter = (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return && !Event.current.shift);

            string[] tabs = { "UAA Profile", "UAA Chat", "UAA Command" };
            SelectedTab = GUILayout.Toolbar(SelectedTab, tabs);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(15);
                _scrollPositionGeneral = EditorGUILayout.BeginScrollView(_scrollPositionGeneral, GUILayout.Height(this.position.height - 30));
                switch (SelectedTab)
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
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
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
                OpenAIURL = EditorGUILayout.TextField("Open AI URL", OpenAIURL);
                OpenAI_API_Key = EditorGUILayout.TextField("Open AI API KEY", OpenAI_API_Key);
                SelectedOpenAIModel = (OpenAIModels)EditorGUILayout.EnumPopup("Open AI Model", SelectedOpenAIModel, GUILayout.ExpandWidth(true));
                OpenAI_API_model = _modelToString[SelectedOpenAIModel];

            }

            MaxIterationsBeforeRestarting = EditorGUILayout.IntSlider("Max Iteration Before Restarting", MaxIterationsBeforeRestarting, 2, 15);
            EditorGUILayout.LabelField("Profile Description");

            _scrollPositionSystemMessage = EditorGUILayout.BeginScrollView(_scrollPositionSystemMessage, GUILayout.Height(200));
            SystemMessage = GUILayout.TextArea(SystemMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Chat-History Naming Task");
            _scrollPositionTitlePrompt = EditorGUILayout.BeginScrollView(_scrollPositionTitlePrompt, GUILayout.Height(75));
            TitlePrompt = GUILayout.TextArea(TitlePrompt, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Command simplification Task");
            _scrollPositionSimplifyCommandToTasksPrompt = EditorGUILayout.BeginScrollView(_scrollPositionSimplifyCommandToTasksPrompt, GUILayout.Height(200));
            SimplifyCommandToTasksPrompt = GUILayout.TextArea(SimplifyCommandToTasksPrompt, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Code Generation Task");
            _scrollPositionTaskToScriptPrompt = EditorGUILayout.BeginScrollView(_scrollPositionTaskToScriptPrompt, GUILayout.Height(400));
            TaskToScriptPrompt = GUILayout.TextArea(TaskToScriptPrompt, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Script Debugging Task");
            _scrollPositionCorrectScriptPrompt = EditorGUILayout.BeginScrollView(_scrollPositionCorrectScriptPrompt, GUILayout.Height(75));
            CorrectScriptPrompt = GUILayout.TextArea(CorrectScriptPrompt, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void ChatTab()
        {
            EditorGUILayout.LabelField("Chat Options", EditorStyles.boldLabel);
            if (GUILayout.Button("Initialize New Chat"))
            {
                _ = UAAChat.InitializeNewChat();
            }

            if (GUILayout.Button("Save Chat History"))
            {
                UAAChat.SaveChatHistory();
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

            if (GUILayout.Button("Load Chat History"))
            {
                UAAChat.LoadChatHistory();
            }

            EditorGUILayout.LabelField("Chat History", EditorStyles.boldLabel);
            _scrollPositionChatHistory = EditorGUILayout.BeginScrollView(_scrollPositionChatHistory, GUILayout.Height(640));

            _totalHeight = -600f;

            for (int i = 1; i < ChatHistory.Count; i++)
            {
                Message message = ChatHistory[i];

                string messageRole;
                if (i == 1)
                    messageRole = message.role + ": ";
                else
                    messageRole = "\n" + message.role + ": ";

                _roleStyle.normal.textColor = message.role == "user" ? Color.magenta : Color.cyan;
                GUIContent role = new GUIContent(messageRole);
                float roleHeight = _roleStyle.CalcHeight(role, EditorGUIUtility.currentViewWidth);
                Rect roleRect = GUILayoutUtility.GetRect(role, _roleStyle, GUILayout.Height(roleHeight));
                //EditorGUI.DrawRect(roleRect, ChatHistoryColor);
                EditorGUI.SelectableLabel(roleRect, messageRole, _roleStyle);

                GUIContent content = new GUIContent(message.content);
                float contentHeight = _messageStyle.CalcHeight(content, EditorGUIUtility.currentViewWidth) + _messagesPadding;
                Rect contentRect = GUILayoutUtility.GetRect(content, _messageStyle, GUILayout.Height(contentHeight));
                EditorGUI.DrawRect(contentRect, ChatHistoryColor);
                EditorGUI.SelectableLabel(contentRect, message.content, _messageStyle);

                _totalHeight += roleHeight + contentHeight;
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

            if (GUILayout.Button("Stop Generating"))
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

            if (GUILayout.Button("Stop Generating"))
            {
                StopGenerating();
            }

            EditorGUILayout.LabelField("Current Task's Script...", EditorStyles.boldLabel);
            _scrollPositionGeneratedScript = EditorGUILayout.BeginScrollView(_scrollPositionGeneratedScript, GUILayout.Height(600));
            GeneratedString = GUILayout.TextArea(GeneratedString, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Validate Step...", EditorStyles.boldLabel);

            if (GUILayout.Button("Execute Script"))
                UAACommand.ExecuteScript();

            if (GUILayout.Button("Ask Assistant to correct Script"))
                _ = UAACommand.CorrectScript();

            if (GUILayout.Button("Delete Generated Script"))
                UAACommand.DeleteGeneratedScript();

            if (GUILayout.Button("Save Command History"))
                UAAChat.SaveChatHistory(setIndex: false, isCommand: true);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        #endregion --------------------------------------------------------------------------------------------------------------------------------

        private void OnEnable()
        {
            _backgroundColor = Settings.GUIBackgroundColor;
            ChatHistoryColor = new(0.1f, 0.1f, 0.1f);

            if (string.IsNullOrEmpty(UserChatMessage))
                UserChatMessage = UserChatMessage;
            if (string.IsNullOrEmpty(UserCommandMessage))
                UserCommandMessage = UserCommandMessage;

            UAAChat.RefreshChatHistory();
            if (CallOnAwake)
                _ = UAAChat.InitializeNewChat();
        }

        public static async Task LLMChat(bool isCommand = false, bool forceNonStream = false)
        {
            IsLLMAvailable = false;
            var messages = isCommand ? CommandHistory : ChatHistory;

            var llmInput = new LocalLLMRequestInput
            {
                messages = messages,
                temperature = Temperature,
                max_tokens = MaxTokens,
                stream = !forceNonStream && Stream
            };
            if (llmInput.stream)
            {
                await UAAConnection.SendAndReceiveStreamedMessages(llmInput, messageContent =>
                {
                    RemoveAssistantStreamingMessages(messages);
                    messages.Add(new Message { role = "assistant", content = messageContent });

                    if (_scrollPositionChatHistory.y > _totalHeight * 0.75 && !isCommand)
                        _scrollPositionChatHistory.y = Mathf.Infinity;
                });
            }
            else
            {
                messages.Add(new Message { role = "assistant", content = "Crafting a response…" });
                string messageContent = await UAAConnection.SendAndReceiveNonStreamedMessages(llmInput);
                messages.RemoveAt(messages.Count - 1);
                messages.Add(new Message { role = Role.assistant.ToString(), content = messageContent });
            }

            if (!isCommand)
                _scrollPositionChatHistory.y = Mathf.Infinity;

            IsLLMAvailable = true;
        }

        private static void RemoveAssistantStreamingMessages(List<Message> messages)
        {
            if (messages.Last().role == "assistant")
                messages.RemoveAt(messages.Count - 1);
        }

        private async void SendMessage()
        {
            if (IsLLMAvailable == false)
                return;

            if (ChatHistory.Count == 0)
                await UAAChat.InitializeNewChat(false);

            if (string.IsNullOrEmpty(UserChatMessage))
                UserChatMessage = UAADefaultPrompts.DefaultUserChatMessage;

            ChatHistory.Add(new Message { role = "user", content = UserChatMessage });
            UserChatMessage = "";
            _ = LLMChat();
        }

        private void SendCommand()
        {
            if (IsLLMAvailable == false)
                return;

            UAACommand.InitializeNewCommand();
        }

        private void StopGenerating()
        {
            IsLLMAvailable = true;
            UAAConnection.StopGenerating();

            if (SelectedTab != 2)
                return;

            UAACommand.IsCommandAborted = true;
            UAACommand.IsCorrectingScript = false;
            UAACommand.CorrectingState = CorrectingStates.NotFixing;
            
        }

        private static void LogMessages(List<Message> messages)
        {
            foreach (var message in messages)
            {
                Debug.Log(message.role + ": " + message.content);
            }
        }

        private void OnDestroy()
        {
            if (SaveOnClose)
                UAAChat.SaveChatHistory();

            UAACommand.UnsubscribeFromEvents();
        }
    }
}



