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
    [SerializeField, TextArea(3, 10)] private string _systemMessage = "Always answer in rhymes.";
    [SerializeField, TextArea(3, 10)] private string _userMessage = "Introduce yourself.";
    [SerializeField, Range(0, 1)] float _temperature = 0.7f;
    [SerializeField] int _max_tokens = -1;
    [SerializeField] bool _stream = false;

    private string _assistantMessage = "we are talking about the weather";
    private List<Message> _chatHistory = new List<Message>();



    public void Start() => InitializeChat();

    private void InitializeChat()
    {
        StartCoroutine(LLMChat(_systemMessage, " ", _userMessage));
    }


    [Button]
    public void SendMessage()
    {
        StartCoroutine(LLMChat(_systemMessage, _assistantMessage, _userMessage));
    }


    private IEnumerator LLMChat(string systemMessage, string assistantMessage, string userMessage)
    {

        float temperature = _temperature;
        int max_tokens = _max_tokens;
        bool stream = _stream;

        var llm = UnityWebRequest.PostWwwForm(url, "POST");
        string jsonMessage = JsonConvert.SerializeObject(new LLMInput
        {
            messages = new List<Message>
            {
                new Message
                {
                    role = "system",
                    content = systemMessage
                },
                new Message
                {
                    role = "user",
                    content = userMessage
                },
                new Message
                {
                    role = "assistant",
                    content = assistantMessage
                }
            },
            temperature = temperature,
            max_tokens = max_tokens,
            stream = stream
        });

        byte[] bytesMessage = Encoding.UTF8.GetBytes(jsonMessage);
        llm.uploadHandler = new UploadHandlerRaw(bytesMessage);
        llm.SetRequestHeader("Content-Type", "application/json");

        _output.text = "Sending message...";

        yield return llm.SendWebRequest();

        if (llm.result == UnityWebRequest.Result.Success)
        {
            var jsonResponse = JsonUtility.FromJson<Response>(llm.downloadHandler.text);
            string messageContent = jsonResponse.choices[0].message.content;
            _output.text = messageContent;
        }

        else
            _output.text = "Error: " + llm.error;
    }

}



