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

public class LLMConnection
{
    private static string Url => LLMChatBot.LocalLLM ? LLMChatBot.LocalURL : LLMChatBot.OpenAiURL;
    private static string OpenAI_API_Key => LLMChatBot.OpenAI_API_Key;
    private static string OpenAI_API_model => LLMChatBot.OpenAI_API_model;

    public static LocalLLMInput CreateLLMInput(string systemPrompt, string userPrompt)
    {
        return new LocalLLMInput
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

            temperature = LLMChatBot.Temperature,
            max_tokens = LLMChatBot.MaxTokens,
            stream = LLMChatBot.Stream,
        };
    }


    public static async Task<string> SendAndReceiveNonStreamedMessages(LocalLLMInput llmInput)
    {
        var post = UnityWebRequest.PostWwwForm(Url, "POST");
        string jsonMessage;

        if (!LLMChatBot.LocalLLM)
        {
            jsonMessage = JsonConvert.SerializeObject(new OpenAIRequest
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


        // TODO I am not so sure about the tcs thing but it seems to work for now I need to check if i can remove it later on?

        var tcs = new TaskCompletionSource<bool>();
        post.SendWebRequest();
        while (!post.isDone)
        {
            await Task.Delay(10);
        }
        tcs.SetResult(true);

        await tcs.Task;

        if (post.result == UnityWebRequest.Result.Success)
        {
            var jsonResponse = JsonUtility.FromJson<Response>(post.downloadHandler.text);
            var messageContent = jsonResponse.choices[0].message.content;

            return messageContent;
        }

        else
            return "Error: " + post.error;
    }

    public static async Task SendAndReceiveStreamedMessages(LocalLLMInput llmInput, Action<string> callback)
    {
        HttpClient client = new HttpClient();

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Url);
        string jsonMessage = "";

        if (!LLMChatBot.LocalLLM)
        {
            jsonMessage = JsonConvert.SerializeObject(new OpenAIRequest
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
        HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync();

            byte[] buffer = new byte[8192];
            int bytesRead;
            string messageContent = "";

            var isStillStreaming = true;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0 && isStillStreaming)
            {
                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (!LLMChatBot.LocalLLM)
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
                            
                            if (delta.IsEmpty())
                                isStillStreaming = false;

                            messageContent += delta.content;
                            callback(messageContent);
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
                        var jsonResponse = JsonUtility.FromJson<Response>(chunk);
                        var delta = jsonResponse.choices[0].delta;

                        if (delta.IsEmpty())
                            break;

                        messageContent += delta.content;
                        callback(messageContent);
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

    internal static void StopGenerating()
    {

    }
}
