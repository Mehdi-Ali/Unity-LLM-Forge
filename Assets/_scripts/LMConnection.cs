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

public static class LLMConnection
{

    public static async Task<string> SendAndReceiveNonStreamedMessages(LLMInput llmInput, string url)
    {
        var llm = UnityWebRequest.PostWwwForm(url, "POST");
        string jsonMessage = JsonConvert.SerializeObject(llmInput);

        byte[] bytesMessage = Encoding.UTF8.GetBytes(jsonMessage);
        llm.uploadHandler = new UploadHandlerRaw(bytesMessage);
        llm.SetRequestHeader("Content-Type", "application/json");

        var tcs = new TaskCompletionSource<bool>();
        llm.SendWebRequest();
        while (!llm.isDone)
        {
            await Task.Delay(100);
        }
        tcs.SetResult(true);

        await tcs.Task;

        if (llm.result == UnityWebRequest.Result.Success)
        {
            var jsonResponse = JsonUtility.FromJson<Response>(llm.downloadHandler.text);
            string messageContent = jsonResponse.choices[0].message.content;
            return messageContent;
        }

        else
            return "Error: " + llm.error;
    }

    public static async Task SendAndReceiveStreamedMessages(LLMInput llmInput, string url, Action<string> callback)
    {
        HttpClient client = new HttpClient();
        string jsonMessage = JsonConvert.SerializeObject(llmInput);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

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
