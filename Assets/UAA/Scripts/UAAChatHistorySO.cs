using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatHistory", menuName = "ScriptableObjects/UnityLMForge/ChatHistory", order = 1)]
public class UAAChatHistorySO : ScriptableObject
{
    public List<Message> ChatHistory;
}
