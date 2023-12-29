using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;


namespace UAA
{
    public class UAAChat
    {
        public static UAASettingsSO Settings;

        public static string ChatHistoryFolderPath { get => Settings.ChatHistoryFolderPath; set => Settings.ChatHistoryFolderPath = value; }

        static UAAChat()
        {
            if (Settings == null)
                Settings = AssetDatabase.LoadAssetAtPath<UAASettingsSO>("Assets/UAA/Settings/UAASettings.asset");
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

                SaveChatHistory(UAAWindow.ChatHistory, false);
            }

            UAAWindow.ChatHistory = new List<Message>(UAAWindow.SavedChatHistory.ChatHistory);
        }

        public static async Task InitializeNewChat()
        {
            UAAWindow.selectedChatHistoryIndex = -1;

            if (UAAWindow.SaveOnNewChat == true)
                SaveChatHistory(UAAWindow.ChatHistory, false);

            while (!UAAWindow.IsLLMAvailable)
            {
                await Task.Delay(100);
            }

            UAAWindow.ChatHistory.Clear();
            UAAWindow.ChatHistory.Add(new Message { role = "system", content = UAAWindow.SystemMessage });

            if (String.IsNullOrEmpty(UAAWindow.UserChatMessage))
                UAAWindow.UserChatMessage = UAAWindow.UserChatMessage;

            UAAWindow.ChatHistory.Add(new Message { role = "user", content = UAAWindow.UserChatMessage });
            UAAWindow.UserChatMessage = "";
            _ = UAAWindow.LLMChat();

            await Task.Delay(1);
        }

        public static void SaveChatHistory(List<Message> messages, bool setIndex = true)
        {
            if (messages.Count <= 0)
                return;
            var saveSlot = UAAWindow.CreateInstance<UAAChatHistorySO>();
            saveSlot.ChatHistory = new(messages);
            string dateTime = DateTime.Now.ToString("yy-MM-dd HH-mm");

            string assetPath = ChatHistoryFolderPath + $"/{dateTime} Chat History.asset";
            AssetDatabase.CreateAsset(saveSlot, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();


            if (setIndex)
                UAAWindow.selectedChatHistoryIndex = UAAWindow.SavedChatHistoryPaths.Length;

            _ = RenameChatHistory(dateTime, assetPath);

        }

        private static async Task RenameChatHistory(string dateTime, string assetPath)
        {

            UAAWindow.ChatHistory.Add(new Message { role = "user", content = Settings.TitlePrompt });
            await UAAWindow.LLMChat();

            string chatHistoryName = CleanAssetName(UAAWindow.ChatHistory[UAAWindow.ChatHistory.Count - 1].content);

            UAAWindow.ChatHistory.RemoveAt(UAAWindow.ChatHistory.Count - 1);
            UAAWindow.ChatHistory.RemoveAt(UAAWindow.ChatHistory.Count - 1);
            AssetDatabase.RenameAsset(assetPath, $"{dateTime} {chatHistoryName}.asset");

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