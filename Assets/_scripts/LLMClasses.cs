using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Response
{
    public string id;
    public Choice[] choices;
}

[Serializable]
public class Choice
{
    public Delta delta;
    public Message message;
}

[System.Serializable]
public struct OpenAIResponse
{
    public string id;
    public Choice[] choices;
}


[Serializable]
public class LocalLLMInput
{
    public List<Message> messages;
    public double temperature;
    public int max_tokens;
    public bool stream;
}

[Serializable]
public struct OpenAIRequest
{
    public string model;
    public Message[] messages;
}

[Serializable]
public class Message
{
    public string role;
    [TextArea(3, 1000)] public string content;
}

[Serializable]
public class Delta
{
    public string role;
    public string content;
    public bool IsEmpty() => string.IsNullOrEmpty(role);
}

[Serializable]
public enum Role
{
    system,
    user,
    assistant
}

