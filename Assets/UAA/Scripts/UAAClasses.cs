using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Response
{
    public string id;
    public List<Choice> choices;
}

[Serializable]
public class Choice
{
    public Delta delta;
    public Message message;
}

[Serializable]
public class OpenAIResponse
{
    public string id;
    public List<OpenAIChoice> choices;
}

[Serializable]
public class OpenAIChoice
{
    public int index;
    public OpenAIDelta delta;
}

[Serializable]
public class OpenAIDelta
{
    public string content;
    public bool IsEmpty() => string.IsNullOrEmpty(content);
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
    public List<Message> messages;
    public bool stream;
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

