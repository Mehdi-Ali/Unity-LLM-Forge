using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UAA
{
    [CreateAssetMenu(fileName = "UAAChatHistory", menuName = "ScriptableObjects/UAA/ChatHistory", order = 1)]
    public class UAAChatHistorySO : ScriptableObject
    {
        public List<Message> ChatHistory;
    }
}
