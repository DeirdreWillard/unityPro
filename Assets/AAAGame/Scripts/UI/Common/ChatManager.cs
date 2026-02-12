
using NetMsg;
using System.Collections.Generic;
using System.Linq;
using GameFramework;
using Cysharp.Threading.Tasks;
using UnityEngine;
public class ChatManager
{
    private static ChatManager instance;
    private ChatManager() { }

    // 快捷语音编码分隔符
    private const string VOICE_SEPARATOR = "#VOICE#";

    public static ChatManager GetInstance()
    {
        if (instance == null)
        {
            instance = new ChatManager();
        }
        return instance;
    }

    /// <summary>
    /// 编码快捷语音数据(索引#VOICE#文本)
    /// </summary>
    public static string EncodeQuickVoice(int voiceIndex, string text = "")
    {
        return $"{voiceIndex}{VOICE_SEPARATOR}{text}";
    }

    /// <summary>
    /// 解码快捷语音数据
    /// </summary>
    /// <param name="encodedData">编码后的数据</param>
    /// <param name="voiceIndex">输出语音索引</param>
    /// <param name="text">输出文本</param>
    /// <returns>是否为快捷语音格式</returns>
    public static bool DecodeQuickVoice(string encodedData, out int voiceIndex, out string text)
    {
        voiceIndex = -1;
        text = encodedData;
        
        if (string.IsNullOrEmpty(encodedData))
            return false;

        int separatorIndex = encodedData.IndexOf(VOICE_SEPARATOR);
        if (separatorIndex <= 0)
            return false;

        string indexStr = encodedData.Substring(0, separatorIndex);
        if (!int.TryParse(indexStr, out voiceIndex))
            return false;

        text = encodedData.Substring(separatorIndex + VOICE_SEPARATOR.Length);
        return true;
    }

    public async void Init()
    {
        //游戏外聊天
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynChatMsg, Function_ChatMsgRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ChatMsgRs, Function_SynChatMsg);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynChatMsgState, Function_SynChatMsgState);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynChatMsgStateList, Function_SynChatMsgStateList);

        //游戏内聊天
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskChat, Function_SynDeskChat);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskChatHistory, Function_SynDeskChatHistory);
        //预加载聊天界面
        // int serialId = await GF.UI.OpenUIFormAwait(UIViews.ChatPanel);
        // GF.UI.CloseUIForm(serialId);

        await UniTask.Delay(200);    // 延时 1 秒
    }

    public void Clear()
    {
        deskChatList.Clear();
        chatMsgList.Clear();
        clubChatNumDic.Clear();
    }

    #region 游戏内聊天

    private List<Msg_DeskChat> deskChatList = new();
    public List<Msg_DeskChat> GetDeskChatList()
    {
        return deskChatList;
    }

    public void AddToDeskChatList(Msg_DeskChat deskChatList)
    {
        GetDeskChatList().Add(deskChatList);
        LimitDeskChatList();
    }

    public void LimitDeskChatList()
    {
        while (GetDeskChatList().Count > 10)
        {
            GetDeskChatList().RemoveAt(0);
        }
    }
    /// <summary>
    /// 桌子内聊天
    /// </summary>
    public void SendText(string text)
    {
        Send_DeskChatRq(0, text);
    }
    
    /// <summary>
    /// 快捷语音聊天(带索引)
    /// </summary>
    /// <param name="voiceIndex">语音索引</param>
    /// <param name="text">显示文本(可选)</param>
    public void SendQuickVoice(int voiceIndex, string text = "")
    {
        string encodedData = EncodeQuickVoice(voiceIndex, text);
        Send_DeskChatRq(1, encodedData);
    }
    
    /// <summary> 表情聊天
    /// </summary>
    public void SendEmoji(string text)
    {
        Send_DeskChatRq(2, text);
    }
    public void Send_DeskChatRq(int type, string chat = "", byte[] voice = null)
    {
        Msg_DeskChatRq req = MessagePool.Instance.Fetch<Msg_DeskChatRq>();
        req.Type = type;
        req.Chat = chat;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DeskChatRq, req);
    }

    /// <summary>
    /// 广播聊天信息
    /// </summary>

    public void Function_SynDeskChat(MessageRecvData data)
    {
        //Msg_BringOptionRs
        GF.LogInfo("广播聊天信息");
        Msg_SynDeskChat ack = Msg_SynDeskChat.Parser.ParseFrom(data.Data);

        Msg_DeskChat chatToForward = ack.Chat;
        
        // 解码快捷语音数据
        if (ack.Chat.Type == 1) // 快捷文字/语音类型
        {
            if (DecodeQuickVoice(ack.Chat.Chat, out int voiceIndex, out string text))
            {
                GF.LogInfo($"收到快捷语音消息 - 索引:{voiceIndex}, 文本:{text}");
                
                // 播放俏皮话语音 - 根据本地存储的男女声设置
                // voiceIndex范围: 0-7, 对应音频文件: 01-08
                int audioIndex = voiceIndex + 1; // 转换为1-8
                
                if (audioIndex >= 1 && audioIndex <= 8)
                {
                    // 读取本地存储的语音性别设置,转换为路径格式: male->MAN, female->WOMAN
                    string voiceGender = PlayerPrefs.GetString("MahjongVoiceGender", "male");
                    string gender = voiceGender == "female" ? "WOMAN" : "MAN";
                    string audioKey = $"PDK_PT_{gender}_QIAOPIHUA_{audioIndex:D2}";
                    string audioPath = AudioKeys.Get(audioKey);
                    
                    if (!string.IsNullOrEmpty(audioPath))
                    {
                        Sound.PlayEffect(audioPath);
                        GF.LogInfo($"播放快捷语音: {audioKey} -> {audioPath}");
                    }
                    else
                    {
                        GF.LogError($"找不到音频资源: {audioKey}");
                    }
                }
                else
                {
                    GF.LogError($"快捷语音索引超出范围: {voiceIndex} (应该在0-7之间)");
                }
                
                // 克隆消息对象,只转发文本内容(不包含索引编码)
                chatToForward = ack.Chat.Clone();
                chatToForward.Chat = text;
            }
        }

        AddToDeskChatList(chatToForward);
        GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_SynDeskChat, chatToForward));
        //byte[] byteArray = req.Voice.ToByteArray();
    }

    /// <summary>
    /// 推送历史桌子聊天信息
    /// </summary>
    public void Function_SynDeskChatHistory(MessageRecvData data)
    {
        Msg_SynDeskChatHistory ack = Msg_SynDeskChatHistory.Parser.ParseFrom(data.Data);
        GF.LogInfo("推送历史桌子聊天信息" , ack.ToString());
        deskChatList = ack.Chat.ToList();
        LimitDeskChatList();
    }



    #endregion


    #region 大厅聊天
    private Dictionary<long, int> clubChatNumDic = new();
    private List<Msg_ChatMsg> chatMsgList = new();
    public List<Msg_ChatMsg> GetChatMsgList()
    {
        return chatMsgList;
    }

    /// <summary>
    /// 获得所有俱乐部对应未读信息条数
    /// </summary>
    /// <param name="clubID"></param>
    /// <param name="num"></param>
    public int GetAllClubChatNum()
    {
        int num = 0;
        foreach (var item in clubChatNumDic)
        {
            num += item.Value;
        }
        // GF.LogError(num + "");
        return num;
    }

    /// <summary>
    /// 获得俱乐部对应未读信息条数
    /// </summary>
    /// <param name="clubID"></param>
    /// <param name="num"></param>
    public void GetClubChatNumByClubID(long clubID, out int num)
    {
        num = clubChatNumDic.ContainsKey(clubID) ? clubChatNumDic[clubID] : 0;
    }

    /// <summary>
    /// 设置俱乐部对应未读信息条数
    /// </summary>
    /// <param name="clubID"></param>
    /// <param name="num"></param>
    public void SetClubChatNumByClubID(long clubID, int num)
    {
        if (clubChatNumDic.ContainsKey(clubID))
        {
            clubChatNumDic[clubID] = num;
        }
        else
        {
            clubChatNumDic.Add(clubID, num);
        }
        GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_SynClubChatNum));
    }

    public void AddToChatMsgList(Msg_ChatMsg chatMsg)
    {
        GetChatMsgList().Add(chatMsg);
        LimitChatMsgList();
    }
    public void AddToChatMsgList(Msg_ChatMsg[] chatMsgList)
    {
        GetChatMsgList().Clear();
        // 按时间排序
        var sortedList = chatMsgList.OrderBy(x => x.Time).ToArray();
        for (int i = 0; i < sortedList.Length; i++)
        {
            GetChatMsgList().Add(sortedList[i]);
        }
    }

    public void LimitChatMsgList()
    {
        while (GetChatMsgList().Count > 10)
        {
            GetChatMsgList().RemoveAt(0);
        }
    }

    public void Send_ChatMsgRq(string msg, long clubId)
    {
        Msg_ChatMsgRq req = MessagePool.Instance.Fetch<Msg_ChatMsgRq>();
        req.Msg = msg;
        //req.Receiver = receiver;
        req.ClubId = clubId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ChatMsgRq, req);
    }
    public void Send_ChatMsgListRq(long clubId)
    {
        Msg_ChatMsgListRq req = MessagePool.Instance.Fetch<Msg_ChatMsgListRq>();
        req.ClubId = clubId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ChatMsgListRq, req);
    }

    public void Function_ChatMsgRs(MessageRecvData data)
    {
        //Msg_BringOptionRs
        Msg_SynChatMsg ack = Msg_SynChatMsg.Parser.ParseFrom(data.Data);
        AddToChatMsgList(ack.Msg);
        GF.LogInfo("Msg_SynChatMsg收到俱乐部聊天信息通知:" , ack.ToString());
        GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_SynClubChat, ack.Msg));
    }

    public void Function_SynChatMsg(MessageRecvData data)
    {
        //Msg_BringOptionRs
        Msg_ChatMsgRs ack = Msg_ChatMsgRs.Parser.ParseFrom(data.Data);
        AddToChatMsgList(ack.Msg);
        GF.LogInfo("Msg_ChatMsgRs收到俱乐部聊天信息返回:" , ack.ToString());
        GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_SynClubChat, ack.Msg));
    }

    public void Function_SynChatMsgState(MessageRecvData data)
    {
        //Msg_SynChatMsgState
        Msg_SynChatMsgState ack = Msg_SynChatMsgState.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynChatMsg推送聊天未读消息数量:" , ack.ToString());
        if (clubChatNumDic.ContainsKey(ack.ClubId))
        {
            clubChatNumDic[ack.ClubId] = ack.Num;
        }
        else
        {
            clubChatNumDic.Add(ack.ClubId, ack.Num);
        }
        GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_SynClubChatNum));
    }

    public void Function_SynChatMsgStateList(MessageRecvData data)
    {
        //Msg_SynChatMsgStateList
        Msg_SynChatMsgStateList ack = Msg_SynChatMsgStateList.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynChatMsgStateList登录推送聊天未读消息数量:" , ack.ToString());
        clubChatNumDic = new Dictionary<long, int>();
        for (int i = 0; i < ack.Msgs.Count; i++)
        {
            clubChatNumDic.Add(ack.Msgs[i].Val, ack.Msgs[i].Key);
        }
        GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_SynClubChatNum));
    }

    #endregion
}