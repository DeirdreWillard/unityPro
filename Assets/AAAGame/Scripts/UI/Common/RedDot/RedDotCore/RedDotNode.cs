﻿using System.Collections.Generic;

/// <summary>
/// 红点数据节点
/// </summary>
public class RedDotNode
{
    /// <summary>
    /// 红点名称
    /// </summary>
    public string rdName { get; set; }

    /// <summary>
    /// 红点计数
    /// </summary>
    public int rdCount { get; private set; }

    /// <summary>
    /// 父红点
    /// </summary>
    public RedDotNode parent;

    /// <summary>
    /// 发生变化的回调函数列表
    /// </summary>
    public List<RedDotManager.OnRdCountChange> countChangeFuncs = new List<RedDotManager.OnRdCountChange>();

    /// <summary>
    /// 子红点的字典表
    /// </summary>
    public Dictionary<string, RedDotNode> rdChildrenDic = new Dictionary<string, RedDotNode>();


    #region 内部接口

    /// <summary>
    /// 重新计算该红点的计数
    /// </summary>
    private void CheckRedDotCount()
    {
        //该红点的计数 = 子红点的计数之和
        int num = 0;
        foreach (RedDotNode node in rdChildrenDic.Values){
            GF.LogInfo("红点详细数据   ", node.rdName + "    " + node.rdCount);
            num += node.rdCount;
        }

        // GF.LogInfo( "红点名字: " + rdName + "   oldNum:  " + rdCount + "红点变化:  " + num);
        //红点计数有变化
        if (num != rdCount)
        {
            rdCount = num;
            NotifyRedDotCountChange();
        }

        parent?.CheckRedDotCount();
    }

    /// <summary>
    /// 红点计数变化，通知
    /// </summary>
    private void NotifyRedDotCountChange()
    {
        // GF.LogError("红点计数有变化回调");
        foreach (var callback in countChangeFuncs)
        {
            callback?.Invoke(this);
        }
    }

    #endregion

    #region 外部接口

    /// <summary>
    /// 设置红点的数量
    /// </summary>
    /// <param name="rdCount"></param>
    public void SetRedDotCount(int rdCount)
    {
        //只能对非根节点进行设定
        if (rdChildrenDic.Count > 0)
        {
            GF.LogWarning("不可直接设定根节点的红点数");
            return;
        }

        //设定该红点的计数
        this.rdCount = rdCount;

        NotifyRedDotCountChange();

        parent?.CheckRedDotCount();
    }

    #endregion
}
