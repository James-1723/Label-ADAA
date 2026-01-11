using System;
using System.Collections.Generic;

[Serializable]
public class OpenAIResponse
{
    public string id;
    public string object_type;
    public long created;
    public Choice[] choices;
}

[Serializable]
public class Choice
{
    public Message message;
    public int index;
    public string finish_reason;
}

[Serializable]
public class Message
{
    public string role;
    public string content;
} 