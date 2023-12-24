using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        UnityWebRequest post;

        if (!LLMChatBot.LocalLLM)
        {
            var jsonMessage = JsonConvert.SerializeObject(new OpenAIRequest
            {
                model = OpenAI_API_model,
                messages = llmInput.messages.ToArray()
            });

            post = UnityWebRequest.Post(Url, jsonMessage, "application/json");
            post.timeout = Timeout.Infinite;
            post.SetRequestHeader("Authorization", "Bearer " + OpenAI_API_Key);
        }

        else
        {
            post = UnityWebRequest.PostWwwForm(Url, "POST");
            string jsonMessage = JsonConvert.SerializeObject(llmInput);
            byte[] bytesMessage = Encoding.UTF8.GetBytes(jsonMessage);
            post.uploadHandler = new UploadHandlerRaw(bytesMessage);
            post.SetRequestHeader("Content-Type", "application/json");
        }

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
            string messageContent;
            if (!LLMChatBot.LocalLLM)
            {
                var jsonResponse = JsonUtility.FromJson<OpenAIResponse>(post.downloadHandler.text);
                messageContent = jsonResponse.choices[0].message.content;
            }
            else
            {
                var jsonResponse = JsonUtility.FromJson<Response>(post.downloadHandler.text);
                messageContent = jsonResponse.choices[0].message.content;
            }
            
            return messageContent;
        }

        else
            return "Error: " + post.error;
    }

    public static async Task SendAndReceiveStreamedMessages(LocalLLMInput llmInput, Action<string> callback)
    {
        HttpClient client = new HttpClient();

        if (!LLMChatBot.LocalLLM)
        {
            client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + OpenAI_API_Key);
        }


        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Url);

        string jsonMessage = "";
        if (LLMChatBot.LocalLLM)
        {
            jsonMessage = JsonConvert.SerializeObject(llmInput);
            // this line is not used in OpenAI! so i moved it here 
            request.Content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
        }
        else
            jsonMessage = JsonConvert.SerializeObject(new OpenAIRequest
            {
                model = OpenAI_API_model,
                messages = llmInput.messages.ToArray()
            });


        HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync();

            byte[] buffer = new byte[8192];
            int bytesRead;
            string messageContent = "";

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                chunk = PrepareJason(chunk);
                if (IsValidJson(chunk))
                {
                    var jsonResponse = JsonUtility.FromJson<Response>(chunk);
                    if (jsonResponse.choices[0].delta.IsEmpty())
                        break;

                    messageContent += jsonResponse.choices[0].delta.content;
                    callback(messageContent);
                }
            }
        }
        else
        {
            callback("Error: " + response.StatusCode);
        }
    }

    private static string PrepareJason(string chunk)
    {
        while (!chunk.StartsWith("{"))
        {
            chunk = chunk[1..];
        }
        
        return chunk;
    }

    private static bool IsValidJson(string strInput)
    {
        strInput = strInput.Trim();
        if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
            (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
        {
            try
            {
                var obj = JToken.Parse(strInput);
                return true;
            }
            catch (JsonReaderException jex)
            {
                //Exception in parsing json
                Debug.Log(jex.Message);
                return false;
            }
            catch (Exception ex) //some other exception
            {
                Debug.Log(ex.ToString());
                return false;
            }
        }
        else
        {
            return false;
        }
    }


    internal static void StopGenerating()
    {

    }
}
