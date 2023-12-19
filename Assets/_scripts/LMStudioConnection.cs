using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using EasyButtons;
using System.Collections.Generic;
using Newtonsoft.Json;

public class LMStudioConnection : MonoBehaviour
{
    [SerializeField] private TextMeshPro _output;


    [SerializeField] string url = "http://localhost:1234/v1/chat/completions";
    [SerializeField, TextArea(3, 20)] private string _systemMessage;
    [SerializeField, TextArea(3, 1000)] private string _userMessage;
    [SerializeField, TextArea(3, 1000)] private string _assistantMessage;
    [SerializeField, Range(0, 1)] float _temperature = 0.7f;
    [SerializeField] int _max_tokens = -1;
    [SerializeField] bool _stream = false;

    public  List<Message> _chatHistory = new List<Message>();



    [Button]
    private void InitializeNewChat()
    {
        _chatHistory.Clear();
        _chatHistory.Add(new Message { role = "system", content = _systemMessage });
        _chatHistory.Add(new Message { role = "user", content = _userMessage });
        StartCoroutine(LLMChat());
    }


    [Button]
    public void SendMessage()
    {
        _chatHistory.Add(new Message { role = "user", content = _userMessage });
        StartCoroutine(LLMChat());
    }

    private IEnumerator LLMChat()
    {
        float temperature = _temperature;
        int max_tokens = _max_tokens;
        bool stream = _stream;

        var llm = UnityWebRequest.PostWwwForm(url, "POST");
        string jsonMessage = JsonConvert.SerializeObject(new LLMInput
        {
            messages = _chatHistory,
            temperature = temperature,
            max_tokens = max_tokens,
            stream = stream
        });

        byte[] bytesMessage = Encoding.UTF8.GetBytes(jsonMessage);
        llm.uploadHandler = new UploadHandlerRaw(bytesMessage);
        llm.SetRequestHeader("Content-Type", "application/json");

        DisplayResponse("Sending message...");

        yield return llm.SendWebRequest();

        if (llm.result == UnityWebRequest.Result.Success)
        {
            var jsonResponse = JsonUtility.FromJson<Response>(llm.downloadHandler.text);
            string messageContent = jsonResponse.choices[0].message.content;
            _chatHistory.Add(new Message { role = "assistant", content = messageContent });
            DisplayResponse(messageContent);
        }

        else
            DisplayResponse("Error: " + llm.error);
    }

    private void DisplayResponse(string message)
    {
        _output.text = message;
        _assistantMessage = message;
    }
}



