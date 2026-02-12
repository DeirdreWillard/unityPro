using System;

public class MessageRecvData
{
    public HotfixNetworkClient Client;
    public int MsgID;
    public UInt64 TargetID;
    public UInt32 UesrData;
    public byte[] Data { get; set; }
}
