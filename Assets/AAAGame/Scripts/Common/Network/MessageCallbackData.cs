using System;


public class MessageCallbackData
{
    public int ID;
    public Action<MessageRecvData> Handler;
}
