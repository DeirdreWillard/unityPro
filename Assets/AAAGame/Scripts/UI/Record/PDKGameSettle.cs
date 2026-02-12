using System.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;
using NetMsg;
using UnityEngine.UI;
using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class PDKGameSettle : UIFormBase
{
    public SingleGameRecord singleGameRecord;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        singleGameRecord = SingleGameRecord.Parser.ParseFrom(Params.Get<VarByteArray>("SingleGameRecord"));
        long PlayerTime = Params.Get<VarInt64>("PlayerTime").Value;

        // 设置基本信息
        transform.Find("DeskBaseInfo").GetComponent<Text>().text = $"房号:{singleGameRecord.Record.DeskId}     局数:{PlayerTime}局";
        transform.Find("DeskType").GetComponent<Text>().text = $"{singleGameRecord.Record.DeskName}  {singleGameRecord.Record.BaseCoin}底";
        transform.Find("ClubInfo").GetComponent<Text>().text = $"圈号: {singleGameRecord.Record.LeagueName}";
        transform.Find("Time").GetComponent<Text>().text = Util.MillisecondsToDateString(singleGameRecord.Record.RecordTime, "yyyy-MM-dd HH:mm:ss");

        // 计算大赢家
        var bigWinnerIndexes = CalculateBigWinners(singleGameRecord.PlayerRecord);

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

            // 显示大赢家标识，隐藏炮王标识（PDK没有炮王概念）
            playerTransform.Find("BigWinImg").gameObject.SetActive(bigWinnerIndexes.Contains(i));
            Transform paoKing = playerTransform.Find("PaoKing");
            if (paoKing != null)
            {
                paoKing.gameObject.SetActive(false);
            }

            // PDK游戏显示简化的统计信息（跑得快游戏特有）
            // 可以根据实际需要显示：出牌次数、剩余牌数等
            playerTransform.Find("Info").GetComponent<Text>().text = 
                $"本局得分       <color=#E4B171>{data.Change}</color>\n" +
                $"\n" +
                $"\n" +
                $"\n" +
                $"\n" +
                $"";

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

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }

    public override void OnClickClose()
    {
        if (Util.IsClickLocked()) return;
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
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
