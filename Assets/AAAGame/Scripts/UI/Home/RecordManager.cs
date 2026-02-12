
using UnityGameFramework.Runtime;
using NetMsg;
using System.Collections.Generic;
using System;
using Google.Protobuf;
using System.Linq;

public class RecordManager
{

    public int joinType = 0; //0个人进入 1:俱乐部进入
    private static RecordManager instance;
    private RecordManager() { }

    private Dictionary<DateTime, List<Msg_GameRecord>> gameRecordRsDic = new();

    public static RecordManager GetInstance()
    {
        instance ??= new RecordManager();
        return instance;
    }

    public void Init()
    {
        HotfixNetworkComponent.AddListener(MessageID.My_GameRecordRs, Function_GameRecordRs);
        HotfixNetworkComponent.AddListener(MessageID.SingleGameRecord, Function_SingleGameRecord);
        HotfixNetworkComponent.AddListener(MessageID.Msg_GameRecordDetailRs, Function_GameRecordDetailRs);

        joinType = 0;
    }

    public void Clear()
    {
        gameRecordRsDic = null;
    }
    /// <summary>
    /// 我的战绩
    /// </summary>
    public void Send_MyGameRecordRq()
    {
        GF.LogInfo("发送战绩信息");
        My_GameRecordRq req = MessagePool.Instance.Fetch<My_GameRecordRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.My_GameRecordRq, req);
    }
    public void Function_GameRecordRs(MessageRecvData data)
    {
        //My_GameRecordRs
        My_GameRecordRs ack = My_GameRecordRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到战绩信息", ack.ToString());

        if (gameRecordRsDic == null)
        {
            gameRecordRsDic = new();
        }

        for (int i = 0; i < ack.Record.Count; i++)
        {
            DateTime dateTime = UtilityBuiltin.UnixTimeStampToDateTime(ack.Record[i].RecordTime).Date;
            gameRecordRsDic.Remove(dateTime);
        }

        for (int i = 0; i < ack.Record.Count; i++)
        {
            AddToGameRecordRsDic(ack.Record[i]);
        }
    }

    /// <summary>
    /// 我的战绩单局
    /// </summary>
    public void Function_SingleGameRecord(MessageRecvData data)
    {
        SingleGameRecord ack = SingleGameRecord.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到单局战绩", ack.ToString());
        //麻将战绩待处理
        OpenGameRecordDetailUI(ack);
    }

    /// <summary>
    /// 我的战绩详情
    /// </summary>
    public void Send_GameRecordDetailRq(long id)
    {
        GF.LogInfo("发送战绩信息");
        Msg_GameRecordDetailRq req = MessagePool.Instance.Fetch<Msg_GameRecordDetailRq>();
        req.RecordId = id;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GameRecordDetailRq, req);
    }


    public void Function_GameRecordDetailRs(MessageRecvData data)
    {
        Msg_GameRecordDetailRs ack = Msg_GameRecordDetailRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到战绩详情", ack.ToString());
        OpenGameRecordDetailUI(ack.Record);
    }

    public async void OpenGameRecordDetailUI(SingleGameRecord singleGameRecord)
    {
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("SingleGameRecord", singleGameRecord.ToByteArray());

        if (GF.Procedure.CurrentProcedure is MJGameProcedure)
        {
            return;
        }
        // 跑得快游戏中也不打开GameRecordDetail弹窗，避免弹出牛牛等其他游戏的战绩界面
        if (GF.Procedure.CurrentProcedure is PDKProcedures)
        {
            return;
        }
        await GF.UI.OpenUIFormAwait(UIViews.GameRecordDetail, uiParams);
    }

    public Dictionary<DateTime, List<Msg_GameRecord>> GetGameRecordRsDic()
    {
        return gameRecordRsDic;
    }
    
    //忽略麻将和跑的快类型的战绩 如果是空的则需要把时间也去除掉
    public Dictionary<DateTime, List<Msg_GameRecord>> GetGameRecordRsDic_NOMj()
    {
        var filteredDict = new Dictionary<DateTime, List<Msg_GameRecord>>();

        foreach (var entry in gameRecordRsDic)
        {
            var filteredList = entry.Value
                .Where(record => record.MethodType != MethodType.MjSimple && record.MethodType != MethodType.RunFast)
                .ToList();

            if (filteredList.Count > 0)
            {
                filteredDict[entry.Key] = filteredList;
            }
        }

        return filteredDict;

    }

    public void AddToGameRecordRsDic(Msg_GameRecord record)
    {
        DateTime dateTime = UtilityBuiltin.UnixTimeStampToDateTime(record.RecordTime).Date;
        if (!gameRecordRsDic.TryGetValue(dateTime, out var recordList))
        {
            recordList = new List<Msg_GameRecord>();
            gameRecordRsDic[dateTime] = recordList;
        }
        recordList.Add(record);
        SortGameRecordDictionary();
    }
    private void SortGameRecordDictionary()
    {
        // 创建按键 (DateTime) 排序的新字典
        var sortedGameRecordRsDic = gameRecordRsDic.OrderByDescending(entry => entry.Key) // 外层按时间从近到远排序
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value.OrderByDescending(record => record.RecordTime).ToList() // 内层按 recordTime 排序
            );

        // 替换原始字典
        gameRecordRsDic = sortedGameRecordRsDic;
    }
}

