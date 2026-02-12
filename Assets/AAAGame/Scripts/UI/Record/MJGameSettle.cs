using System.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;
using NetMsg;
using UnityEngine.UI;
using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MJGameSettle : UIFormBase
{
    public SingleGameRecord singleGameRecord;
    private MJMethod mJMethod; // 麻将类型: "KWX" = 卡五星, "XTHH" = 仙桃晃晃
    private MethodType methodType = MethodType.MjSimple;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        singleGameRecord = SingleGameRecord.Parser.ParseFrom(Params.Get<VarByteArray>("SingleGameRecord"));
        long PlayerTime = Params.Get<VarInt64>("PlayerTime").Value;

        // 兼容：不同游戏传参不同，跑得快复用本面板时可能不会传 MJMethod
        if (Params.TryGet<VarInt32>("MethodType", out var methodTypeVar))
        {
            methodType = (MethodType)methodTypeVar.Value;
        }
        else
        {
            // 老调用方不传 MethodType 时，尝试从桌名推断
            methodType = (singleGameRecord?.Record?.DeskName != null && singleGameRecord.Record.DeskName.Contains("跑的快"))
                ? MethodType.RunFast
                : MethodType.MjSimple;
        }

        if (Params.TryGet<VarInt32>("MJMethod", out var mjMethodVar))
        {
            mJMethod = (MJMethod)mjMethodVar.Value;
        }
        else
        {
            // 默认仙桃晃晃
            mJMethod = MJMethod.Huanghuang;
        }

        // 设置基本信息
        transform.Find("DeskBaseInfo").GetComponent<Text>().text = $"房号:{singleGameRecord.Record.DeskId}     局数:{PlayerTime}局";
        transform.Find("DeskType").GetComponent<Text>().text = $"{singleGameRecord.Record.DeskName}  {singleGameRecord.Record.BaseCoin}底";
        transform.Find("ClubInfo").GetComponent<Text>().text = $"圈号: {singleGameRecord.Record.LeagueName}";
        transform.Find("Time").GetComponent<Text>().text = Util.MillisecondsToDateString(singleGameRecord.Record.RecordTime, "yyyy-MM-dd HH:mm:ss");

        // 计算大赢家和炮王（跑得快没有炮王概念）
        var bigWinnerIndexes = CalculateBigWinners(singleGameRecord.PlayerRecord);
        var paoKingIndexes = methodType == MethodType.RunFast
            ? new System.Collections.Generic.List<int>()
            : CalculatePaoKings(singleGameRecord.PlayerRecord);

        // 获取玩家数量
        int playerCount = singleGameRecord.PlayerRecord.Count;
        Transform content = transform.Find("PlayerObj");
        // 处理所有玩家位置(最多4个)
        for (int i = 0; i < 4; i++)
        {
            Transform playerTransform = content.Find("Player" + (i + 1));

            // 如果玩家数量少于4个,隐藏多余的位置
            if (i >= playerCount)
            {
                playerTransform.gameObject.SetActive(false);
                continue;
            }

            playerTransform.gameObject.SetActive(true);
            var data = singleGameRecord.PlayerRecord[i];

            // 设置玩家基本信息
            Util.DownloadHeadImage(playerTransform.Find("Head").GetComponent<RawImage>(), data.HeadImage);
            playerTransform.Find("Name").GetComponent<Text>().text = data.Nick;
            playerTransform.Find("ID").GetComponent<Text>().text = $"ID: {data.PlayerId}";

            // 显示大赢家和炮王标识
            playerTransform.Find("BigWinImg").gameObject.SetActive(bigWinnerIndexes.Contains(i));
            var paoKingNode = playerTransform.Find("PaoKing");
            if (paoKingNode != null)
            {
                paoKingNode.gameObject.SetActive(methodType != MethodType.RunFast && paoKingIndexes.Contains(i));
            }

            // 根据游戏类型设置玩家详细信息
            playerTransform.Find("Info").GetComponent<Text>().text =
                methodType == MethodType.RunFast
                    ? GetRunFastPlayerInfoText(data)
                    : GetPlayerInfoText(data);

            // 设置分数显示
            float score = float.Parse(data.Change);
            if (score >= 0)
            {
                playerTransform.Find("WinScore").GetComponent<Text>().text = "+" + data.Change;
                playerTransform.Find("LoseScore").GetComponent<Text>().text = "";
            }
            else
            {
                playerTransform.Find("WinScore").GetComponent<Text>().text = "";
                playerTransform.Find("LoseScore").GetComponent<Text>().text = data.Change;
            }
        }
    }

    private string GetRunFastPlayerInfoText(PlayerGameRecord data)
    {
        // 跑得快复用该面板：显示简化信息
        return $"胜利次数       <color=#E4B171>{data.WinTime}</color>\n" +
               $"炸弹个数       <color=#E4B171>{data.BoomTime}</color>\n" +
               $"最大分数       <color=#E4B171>{data.MaxFan}</color>\n" ;          
    }

    /// <summary>
    /// 根据麻将类型获取玩家详细信息文本
    /// </summary>
    private string GetPlayerInfoText(PlayerGameRecord data)
    {
        switch (mJMethod)
        {
            case MJMethod.Kwx:
                // 卡五星
                return $"大胡次数       <color=#E4B171>{data.BigHu}</color>\n" +
                       $"自摸次数       <color=#E4B171>{data.ZiMo}</color>\n" +
                       $"点炮次数       <color=#E4B171>{data.DianPao}</color>\n" +
                       $"最大分数       <color=#E4B171>{data.MaxFan}</color>";
            case MJMethod.Huanghuang:
                // 仙桃晃晃（默认）
                return $"自摸次数       <color=#E4B171>{data.ZiMo}</color>\n" +
                       $"接炮次数       <color=#E4B171>{data.DianPaoHu}</color>\n" +
                       $"点炮次数       <color=#E4B171>{data.DianPao}</color>\n" +
                       $"暗杠次数       <color=#E4B171>{data.AnGang}</color>\n" +
                       $"明杠次数       <color=#E4B171>{data.MingGang}</color>\n" +
                       $"飘赖次数       <color=#E4B171>{data.Piao}</color>";
            default:
                return ""; // 默认仙桃晃晃
        }
    }

    /// <summary>
    /// 计算大赢家(变化分数最高的玩家,可能有多个)
    /// </summary>
    private System.Collections.Generic.List<int> CalculateBigWinners(Google.Protobuf.Collections.RepeatedField<PlayerGameRecord> playerRecords)
    {
        var bigWinners = new System.Collections.Generic.List<int>();
        
        if (playerRecords == null || playerRecords.Count == 0)
            return bigWinners;

        float maxScore = float.MinValue;

        // 第一遍遍历找出最高分数
        for (int i = 0; i < playerRecords.Count; i++)
        {
            if (float.TryParse(playerRecords[i].Change, out float score))
            {
                if (score > maxScore)
                {
                    maxScore = score;
                }
            }
        }

        // 如果最高分数小于等于0,则没有大赢家
        if (maxScore <= 0)
            return bigWinners;

        // 第二遍遍历找出所有达到最高分数的玩家
        for (int i = 0; i < playerRecords.Count; i++)
        {
            if (float.TryParse(playerRecords[i].Change, out float score))
            {
                if (score == maxScore)
                {
                    bigWinners.Add(i);
                }
            }
        }

        return bigWinners;
    }

    /// <summary>
    /// 计算炮王(点炮次数最多的玩家,可能有多个)
    /// </summary>
    private System.Collections.Generic.List<int> CalculatePaoKings(Google.Protobuf.Collections.RepeatedField<PlayerGameRecord> playerRecords)
    {
        var paoKings = new System.Collections.Generic.List<int>();
        
        if (playerRecords == null || playerRecords.Count == 0)
            return paoKings;

        int maxDianPao = 0;

        // 第一遍遍历找出最多点炮次数
        for (int i = 0; i < playerRecords.Count; i++)
        {
            if (playerRecords[i].DianPao > maxDianPao)
            {
                maxDianPao = playerRecords[i].DianPao;
            }
        }

        // 如果点炮次数为0,则没有炮王
        if (maxDianPao == 0)
            return paoKings;

        // 第二遍遍历找出所有达到最多点炮次数的玩家
        for (int i = 0; i < playerRecords.Count; i++)
        {
            if (playerRecords[i].DianPao == maxDianPao)
            {
                paoKings.Add(i);
            }
        }

        return paoKings;
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }

    public override void OnClickClose()
    {
        if (Util.IsClickLocked()) return;
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (GF.Procedure.CurrentProcedure is MJGameProcedure mJGameProcedure)
        {
            mJGameProcedure.ExitbaseMahjongGameManager();
            return;
        }
         if (GF.Procedure.CurrentProcedure is PDKProcedures pDKProcedures)
        {
            pDKProcedures.ExitPDKGame();
            return;
        }
        GF.UI.Close(this.UIForm);
    }

    public void CopyRecordClick()
    {
        if (Util.IsClickLocked()) return;
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        
        // 构建玩家信息
        string playerInfo = "";
        foreach (var player in singleGameRecord.PlayerRecord)
        {
            playerInfo += $" 【{player.Nick}】 ID：{player.PlayerId} 积分：{Util.FormatAmount(player.Change)}\n";
        }
        
        // 构建完整的战绩信息
        string recordInfo = "幸运星\n" +
                            $"{Util.MillisecondsToDateString(singleGameRecord.Record.RecordTime, "yyyy-MM-dd HH:mm:ss")}\n" +
                            $"{singleGameRecord.Record.DeskName}-{singleGameRecord.Record.BaseCoin}底 俱乐部: {singleGameRecord.Record.LeagueName}\n" +
                           $"{transform.Find("DeskBaseInfo").GetComponent<Text>().text}\n" +
                           $"{playerInfo}" +
                           $"绿色游戏,远离赌博,仅供娱乐,请勿他用!";
        
        GUIUtility.systemCopyBuffer = recordInfo;
        GF.UI.ShowToast("战绩信息已复制到剪贴板");
    }


}