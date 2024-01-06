using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;


namespace UAA
{
    public class UAAConnection
    {
        private static string Url => UAAWindow.LocalLLM ? UAAWindow.LocalURL : UAAWindow.OpenAIURL;
        private static string OpenAI_API_Key => UAAWindow.OpenAI_API_Key;
        private static string OpenAI_API_model => UAAWindow.OpenAI_API_model;

        private static CancellationTokenSource _cts;


        public static LocalLLMRequestInput CreateLLMInput(string systemPrompt, string userPrompt)
        {
            return new LocalLLMRequestInput
            {
                messages = new List<Message>
            {
                new Message
                {
                    role = "system",
                    content = systemPrompt
                },
                new Message
                {
                    role = "user",
                    content = userPrompt
                },
            },

                temperature = UAAWindow.Temperature,
                max_tokens = UAAWindow.MaxTokens,
                stream = UAAWindow.Stream,
            };
        }

        public static async Task<string> SendAndReceiveNonStreamedMessages(LocalLLMRequestInput llmInput)
        {
            UAAWindow.IsLLMAvailable = false;

            var post = UnityWebRequest.PostWwwForm(Url, "POST");
            string jsonMessage;

            if (!UAAWindow.LocalLLM)
            {
                jsonMessage = JsonConvert.SerializeObject(new OpenAIRequestInput
                {
                    model = OpenAI_API_model,
                    messages = llmInput.messages
                });

                post.timeout = Timeout.Infinite;
                post.SetRequestHeader("Authorization", "Bearer " + OpenAI_API_Key);
            }

            else
                jsonMessage = JsonConvert.SerializeObject(llmInput);

            byte[] bytesMessage = Encoding.UTF8.GetBytes(jsonMessage);
            post.uploadHandler = new UploadHandlerRaw(bytesMessage);
            post.SetRequestHeader("Content-Type", "application/json");
            post.SendWebRequest();

            while (!post.isDone)
            {
                await Task.Delay(10);
            }

            UAAWindow.IsLLMAvailable = false;

            if (post.result == UnityWebRequest.Result.Success)
            {
                var jsonResponse = JsonUtility.FromJson<LocalLLMResponse>(post.downloadHandler.text);
                var messageContent = jsonResponse.choices[0].message.content;

                return messageContent.Trim();
            }

            else
                return "Error: " + post.error;
        }

        public static async Task SendAndReceiveStreamedMessages(LocalLLMRequestInput llmInput, Action<string> callback)
        {
            UAAWindow.IsLLMAvailable = false;

            HttpClient client = new();

            HttpRequestMessage request = new(HttpMethod.Post, Url);
            string jsonMessage = "";

            if (!UAAWindow.LocalLLM)
            {
                jsonMessage = JsonConvert.SerializeObject(new OpenAIRequestInput
                {
                    model = OpenAI_API_model,
                    messages = llmInput.messages,
                    stream = true
                });

                client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + OpenAI_API_Key);
            }
            else
                jsonMessage = JsonConvert.SerializeObject(llmInput);

            request.Content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            _cts?.Dispose();
            _cts = new();
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();

                byte[] buffer = new byte[8192];
                int bytesRead;
                StringBuilder messageContent = new StringBuilder();

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                {
                        
                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (!UAAWindow.LocalLLM)
                    {
                        List<string> dataList = SplitJsonObjects(chunk);
                        foreach (var data in dataList)
                        {
                            try
                            {
                                OpenAIResponse jsonResponse = JsonUtility.FromJson<OpenAIResponse>(data);
                                if (jsonResponse == null)
                                    continue;

                                OpenAIDelta delta = jsonResponse.choices[0].delta;
                                if (delta == null)
                                    continue;

                                // if (delta.IsEmpty())
                                //     _isStillStreaming = false;

                                messageContent.Append(delta.content);
                                callback(messageContent.ToString());
                            }

                            catch (Exception)
                            {
                                continue;
                            }

                        }
                    }

                    else
                    {
                        chunk = PrepareJason(chunk);
                        try
                        {
                            var jsonResponse = JsonUtility.FromJson<LocalLLMResponse>(chunk);
                            var delta = jsonResponse.choices[0].delta;

                            if (delta.IsEmpty())
                                break;

                            messageContent.Append(delta.content);
                            callback(messageContent.ToString());
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }
            else
            {
                callback("Error: " + response.StatusCode);
            }

            UAAWindow.IsLLMAvailable = true;
        }

        public static List<string> SplitJsonObjects(string data)
        {
            string pattern = @"{(?:[^{}]|(?<open>{)|(?<-open>}))*(?(open)(?!))}";

            MatchCollection matches = Regex.Matches(data, pattern);

            List<string> dataList = new List<string>();
            foreach (Match match in matches)
            {
                dataList.Add(match.Value);
            }

            return dataList;
        }

        private static string PrepareJason(string chunk)
        {
            while (!chunk.StartsWith("{"))
            {
                chunk = chunk[1..];
            }

            return chunk;
        }

        public static void StopGenerating()
        {
            _cts.Cancel();
            Debug.Log("StopGenerating is not implemented yet");
        }
    }
}
