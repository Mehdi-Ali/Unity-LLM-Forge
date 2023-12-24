using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Response
{
    public Choice[] choices;
}

[Serializable]
public class Choice
{
    public Delta delta;
    public Message message;
}

[Serializable]
public class Message
{
    public string role;
    [TextArea(3, 1000)] public string content;
}

public class LLMInput
{
    public List<Message> messages;
    public double temperature;
    public int max_tokens;
    public bool stream;
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
