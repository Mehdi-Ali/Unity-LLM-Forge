using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public static class LLMConnection
{

    public static async Task<string> SendAndReceiveMessages(LLMInput llmInput, string url)
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
}
