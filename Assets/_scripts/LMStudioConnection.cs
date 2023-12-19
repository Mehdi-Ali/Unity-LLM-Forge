using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using EasyButtons;
using System.Collections.Generic;

public class LMStudioConnection : MonoBehaviour
{
    [SerializeField] private TextMeshPro _output;


    [SerializeField] string url = "http://localhost:1234/v1/chat/completions";
    [SerializeField, TextArea(3,10)] private string _systemMessage = "Always answer in rhymes.";
    [SerializeField, TextArea(3, 10)] private string _userMessage = "Introduce yourself.";
    [SerializeField] const string _temperature = "0.7";
    [SerializeField] const string _max_tokens = "-1";
    [SerializeField] const string _stream = "false";


    private List<string> discussionHistory = new List<string>();



    public void Start()
    {
        StartCoroutine(PostRequest(_systemMessage, _userMessage, _temperature, _max_tokens, _stream));
    }

    [Button]
    public void SendMessage()
    {
        // maybe we don't need to send the system message every time?
        StartCoroutine(PostRequest(_systemMessage, _userMessage, _temperature, _max_tokens, _stream));
    }


    private IEnumerator PostRequest(string systemMessage, string userMessage, string temperature = _temperature, string max_tokens = _max_tokens, string stream = _stream)
    {
        var llm = UnityWebRequest.PostWwwForm(url, "POST");
        string jsonMessage = $@"
                        {{
                        ""messages"": [
                            {{
                            ""role"": ""system"",
                            ""content"": ""{systemMessage}""
                            }},
                            {{
                            ""role"": ""user"",
                            ""content"": ""{userMessage}""
                            }}
                        ],
                        ""temperature"": {temperature},	
                        ""max_tokens"": {max_tokens},
                        ""stream"": {stream}
                        }}";

        byte[] bytesMessage = Encoding.UTF8.GetBytes(jsonMessage);
        llm.uploadHandler = new UploadHandlerRaw(bytesMessage);
        llm.SetRequestHeader("Content-Type", "application/json");

        _output.text = "Sending message...";
        
        yield return llm.SendWebRequest();

        if (llm.result != UnityWebRequest.Result.Success)
            _output.text = "Error: " + llm.error;

        else
        {
            var jsonResponse = JsonUtility.FromJson<Response>(llm.downloadHandler.text);
            string messageContent = jsonResponse.choices[0].message.content;
            _output.text =  messageContent;
        }
    }

    [Serializable]
    public class Response
    {
        public Choice[] choices;
    }

    [Serializable]
    public class Choice
    {
        public Message message;
    }

    [Serializable]
    public class Message
    {
        public string content;
    }
}



