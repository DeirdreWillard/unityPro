using System;
using System.Collections.Generic;
using NetMsg;

/// <summary>
/// 麻将消息缓存管理器
/// 用于在进入游戏流程前缓存服务器发送的消息，避免初始化期间消息丢失
/// </summary>
public static class MJMessageCache
{
    /// <summary>
    /// 缓存的消息数据
    /// </summary>
    private static Queue<(MessageID msgId, byte[] data)> cachedMessages = new Queue<(MessageID, byte[])>();
    
    /// <summary>
    /// 是否正在缓存模式（进入游戏但未初始化完成）
    /// </summary>
    private static bool isCaching = false;
    
    /// <summary>
    /// 需要缓存的消息ID集合
    /// </summary>
    private static HashSet<MessageID> messageIdsToCache = new HashSet<MessageID>
    {
        MessageID.Syn_DeskPlayerInfo,
        MessageID.Syn_DeskPlayerInfo1,
        MessageID.Syn_DeskPlayerInfo2,
        MessageID.Msg_SynSitUp,
        MessageID.PBMJStart,
        MessageID.Msg_MJOption1,
        MessageID.Msg_MJOption2,
        MessageID.Msg_MJOption3,
        MessageID.Msg_MJOption4,
        MessageID.Msg_MJOption5,
        MessageID.Msg_MJOption6,
        MessageID.Msg_MJOption7,
        MessageID.Msg_MJOption8,
        MessageID.Msg_MJOption9,
        MessageID.Syn_HHCoinChange,
        MessageID.Msg_Hu,
        MessageID.Msg_MJReadyRs,
        MessageID.Msg_SynMJReady,
        MessageID.Msg_SynMJDismiss,
        MessageID.MSG_ChangeMJDeskState,
        MessageID.Msg_SynPiaoRs,
        MessageID.Msg_ChangeCardRs,
        MessageID.Msg_SynChangeCardRs,
        MessageID.Msg_SynHandCard,
        MessageID.Msg_SynPlayerHandCard,
        MessageID.Msg_ThrowCardRs,
        MessageID.Msg_XlSettle,
        MessageID.SynPrayRs,
        MessageID.Msg_SynPlayerState,
        MessageID.Syn_PlayerAuto,
        MessageID.Msg_SynSendGift,
    };
    
    /// <summary>
    /// 开始缓存模式（在切换到MJGameProcedure之前调用）
    /// </summary>
    public static void StartCaching()
    {
        isCaching = true;
        cachedMessages.Clear();
        GF.LogInfo("[MJMessageCache] 开始缓存麻将消息");
    }
    
    /// <summary>
    /// 停止缓存模式并返回所有缓存的消息
    /// </summary>
    public static Queue<(MessageID msgId, byte[] data)> StopCachingAndGetMessages()
    {
        isCaching = false;
        var messages = cachedMessages;
        cachedMessages = new Queue<(MessageID, byte[])>();
        
        if (messages.Count > 0)
        {
            GF.LogInfo($"[MJMessageCache] 停止缓存，共有 {messages.Count} 条消息待处理");
        }
        
        return messages;
    }
    
    /// <summary>
    /// 清除缓存（在离开游戏时调用）
    /// </summary>
    public static void Clear()
    {
        isCaching = false;
        cachedMessages.Clear();
    }
    
    /// <summary>
    /// 是否正在缓存模式
    /// </summary>
    public static bool IsCaching => isCaching;
    
    /// <summary>
    /// 尝试缓存消息（由网络层调用）
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="data">消息数据</param>
    /// <returns>是否已缓存（true表示已缓存，调用方不需要分发；false表示未缓存，需要正常分发）</returns>
    public static bool TryCache(MessageID msgId, byte[] data)
    {
        if (!isCaching)
            return false;
            
        if (!messageIdsToCache.Contains(msgId))
            return false;
        
        // 复制数据以避免引用问题
        byte[] dataCopy = new byte[data.Length];
        Array.Copy(data, dataCopy, data.Length);
        
        cachedMessages.Enqueue((msgId, dataCopy));
        GF.LogInfo($"[MJMessageCache] 缓存消息: {msgId}");
        return true;
    }
    
    /// <summary>
    /// 获取缓存的消息数量
    /// </summary>
    public static int CachedCount => cachedMessages.Count;
}
