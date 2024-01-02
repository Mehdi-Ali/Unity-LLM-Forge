using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;


namespace UAA
{
    public class UAAChat
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

        private static string ChatHistoryFolderPath
        {
            get => Settings.ChatHistoryFolderPath;
            set => Settings.ChatHistoryFolderPath = value;
        }


        public static void RefreshChatHistory()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(UAAChatHistorySO)}", new[] { ChatHistoryFolderPath });
            UAAWindow.SavedChatHistoryPaths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                UAAWindow.SavedChatHistoryPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }
        }

        public static void LoadChatHistory()
        {
            UAAChat.RefreshChatHistory();

            if (UAAWindow.SavedChatHistory == null)
            {
                UnityEngine.Debug.LogError("Invalid chat history selected");
                return;
            }

            if (UAAWindow.SaveOnLoad == true)
            {
                var index = 0;
                if (UAAWindow.selectedChatHistoryIndex <= UAAWindow.SavedChatHistoryPaths.Length - 1)
                    index = UAAWindow.selectedChatHistoryIndex;

                SaveChatHistory(setIndex: false);
            }

            UAAWindow.ChatHistory = new List<Message>(UAAWindow.SavedChatHistory.ChatHistory);
        }

        public static async Task InitializeNewChat(bool SendDefaultMessage = true)
        {
            UAAWindow.selectedChatHistoryIndex = -1;

            if (UAAWindow.SaveOnNewChat == true)
                SaveChatHistory(setIndex: false);

            while (!UAAWindow.IsLLMAvailable)
            {
                await Task.Delay(100);
            }

            UAAWindow.ChatHistory.Clear();
            UAAWindow.ChatHistory.Add(new Message { role = "system", content = UAAWindow.SystemMessage });

            if (!SendDefaultMessage)
                return;

            if (string.IsNullOrEmpty(UAAWindow.UserChatMessage))
                UAAWindow.UserChatMessage = UAADefaultPrompts.DefaultUserChatMessage;

            UAAWindow.ChatHistory.Add(new Message { role = "user", content = UAAWindow.UserChatMessage });
            UAAWindow.UserChatMessage = "";
            _ = UAAWindow.LLMChat();
        }

        public static void SaveChatHistory(bool setIndex = true, bool isCommand = false)
        {
            var messages = isCommand ? UAAWindow.CommandHistory : UAAWindow.ChatHistory;

            if (messages.Count <= 0)
                return;
            
            var saveSlot = UAAWindow.CreateInstance<UAAChatHistorySO>();
            saveSlot.ChatHistory = new(messages);
            string dateTime = DateTime.Now.ToString("yy-MM-dd HH-mm");

            string assetPath = ChatHistoryFolderPath + $"/{dateTime} Chat History.asset";
            if (isCommand)
                assetPath = ChatHistoryFolderPath + $"/{dateTime} Command.asset";

            AssetDatabase.CreateAsset(saveSlot, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();


            if (setIndex)
                UAAWindow.selectedChatHistoryIndex = UAAWindow.SavedChatHistoryPaths.Length;

            _ = RenameChatHistory(dateTime, assetPath, isCommand);

        }

        private static async Task RenameChatHistory(string dateTime, string assetPath, bool isCommand = false)
        {
            var messages = isCommand ? UAAWindow.CommandHistory : UAAWindow.ChatHistory;

            messages.Add(new Message { role = Role.user.ToString(), content = Settings.TitlePrompt });

            await UAAWindow.LLMChat(isCommand, forceNonStream: true);

            string chatHistoryName = CleanAssetName(messages[^1].content);

            messages.RemoveAt(messages.Count - 1);
            messages.RemoveAt(messages.Count - 1);

            var assetName = $"{dateTime} {chatHistoryName}.asset";
            if (isCommand)
                assetName = $"{dateTime} Command {chatHistoryName}.asset";
            
            UnityEngine.Debug.Log($"Renaming {assetPath} to {assetName}");

            AssetDatabase.RenameAsset(assetPath, assetName);

            UAAChat.RefreshChatHistory();
        }

        private static string CleanAssetName(string chatHistoryName)
        {
            if (chatHistoryName.StartsWith("Title", StringComparison.OrdinalIgnoreCase))
            {
                chatHistoryName = chatHistoryName.Substring(5).TrimStart();
            }

            chatHistoryName = Regex.Replace(chatHistoryName, "[^a-zA-Z0-9 -]", ""); ;
            return chatHistoryName;
        }
    }
}