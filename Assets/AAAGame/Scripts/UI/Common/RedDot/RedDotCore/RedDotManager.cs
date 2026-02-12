﻿using System;
using System.Collections.Generic;

/// <summary>
/// 红点路径定义
/// </summary>
public static class E_RedDotDefine
{
    /// <summary>
    /// 红点树的根节点
    /// </summary>
    public const string rdRoot = "Root";

    // ---------- 业务红点 ----------
    public const string MsgBox = "Root/Msg";
    public const string MsgBox_All = "Root/Msg/All";
    public const string MsgBox_System = "Root/Msg/System";
    public const string MsgBox_Team = "Root/Msg/Team";
    public const string MsgBox_Coin = "Root/Msg/Coin";
}

/// <summary>
/// 红点系统
/// </summary>
public class RedDotManager
{
    private static RedDotManager m_RedDotManager;

    /// <summary>
    /// 红点管理器
    /// </summary>
    public static RedDotManager GetInstance()
    {
        if (m_RedDotManager == null)
        {
            m_RedDotManager = new RedDotManager();
        }
        return m_RedDotManager;
    }

    public RedDotManager()
    {
        InitRedDotTreeNode();
    }

    /// <summary>
    /// 红点数变化通知委托
    /// </summary>
    /// <param name="node"></param>
    public delegate void OnRdCountChange(RedDotNode node);

    /// <summary>
    /// 红点树的的 Root节点
    /// </summary>
    private RedDotNode mRootNode;

    /// <summary>
    /// 红点路径的表（每次 E_RedDotDefine 添加完后此处也必须添加）
    /// </summary>
    private static List<string> lstRedDotTreeList = new List<string>
        {
            E_RedDotDefine.rdRoot,

            E_RedDotDefine.MsgBox,
            E_RedDotDefine.MsgBox_All,
            E_RedDotDefine.MsgBox_System,
            E_RedDotDefine.MsgBox_Team,
            E_RedDotDefine.MsgBox_Coin,
        };


    #region 内部接口

    /// <summary>
    /// 初始化红点树
    /// </summary>
    private void InitRedDotTreeNode()
    {
        /*
        * 结构层：根据红点是否显示或显示数，自定义红点的表现方式
        */

        mRootNode = new RedDotNode { rdName = E_RedDotDefine.rdRoot };

        foreach (string path in lstRedDotTreeList)
        {
            string[] treeNodeAy = path.Split('/');
            int nodeCount = treeNodeAy.Length;
            RedDotNode curNode = mRootNode;

            if (treeNodeAy[0] != mRootNode.rdName)
            {
                GF.LogError("根节点必须为Root，检查 " + treeNodeAy[0]);
                continue;
            }

            if (nodeCount > 1)
            {
                for (int i = 1; i < nodeCount; i++)
                {
                    if (!curNode.rdChildrenDic.ContainsKey(treeNodeAy[i]))
                    {
                        curNode.rdChildrenDic.Add(treeNodeAy[i], new RedDotNode());
                    }

                    curNode.rdChildrenDic[treeNodeAy[i]].rdName = treeNodeAy[i];
                    curNode.rdChildrenDic[treeNodeAy[i]].parent = curNode;

                    curNode = curNode.rdChildrenDic[treeNodeAy[i]];
                }
            }
        }
    }

    /// <summary>
    /// 移除红点数变化的回调
    /// </summary>
    /// <param name="strNode">红点路径，必须是 RedDotDefine </param>
    /// <param name="callBack">要移除的回调函数</param>
    public void RemoveRedDotNodeCallBack(string strNode, OnRdCountChange callBack)
    {
        var nodeList = strNode.Split('/');

        if (nodeList.Length == 1)
        {
            if (nodeList[0] != E_RedDotDefine.rdRoot)
            {
                GF.LogError("Get Wrong Root Node! current is " + nodeList[0]);
                return;
            }
        }

        var node = mRootNode;
        for (int i = 1; i < nodeList.Length; i++)
        {
            if (!node.rdChildrenDic.ContainsKey(nodeList[i]))
            {
                GF.LogError("Does Not Contain child Node: " + nodeList[i]);
                return;
            }

            node = node.rdChildrenDic[nodeList[i]];

            if (i == nodeList.Length - 1)
            {
                node.countChangeFuncs.Remove(callBack);
                return;
            }
        }
    }

    #endregion

    #region 外部接口

    /// <summary>
    /// 设置红点数变化的回调
    /// </summary>
    /// <param name="strNode">红点路径，必须是 RedDotDefine </param>
    /// <param name="callBack">回调函数</param>
    public void SetRedDotNodeCallBack(string strNode, OnRdCountChange callBack)
    {
        var nodeList = strNode.Split('/');

        if (nodeList.Length == 1)
        {
            if (nodeList[0] != E_RedDotDefine.rdRoot)
            {
                GF.LogError("Get Wrong Root Node! current is " + nodeList[0]);
                return;
            }
        }

        var node = mRootNode;
        for (int i = 1; i < nodeList.Length; i++)
        {
            if (!node.rdChildrenDic.ContainsKey(nodeList[i]))
            {
                GF.LogError("Does Not Contain child Node: " + nodeList[i]);
                return;
            }

            node = node.rdChildrenDic[nodeList[i]];

            if (i == nodeList.Length - 1)
            {
                node.countChangeFuncs.Add(callBack);
                return;
            }
        }
    }

    /// <summary>
    /// 设置红点参数
    /// </summary>
    /// <param name="nodePath">红点路径，必须走 RedDotDefine </param>
    /// <param name="rdCount">红点计数</param>
    public void Set(string nodePath, int rdCount = 0)
    {
        //GF.LogError("nodepath: " + nodePath + "rdcount:  " + rdCount);
        string[] nodeList = nodePath.Split('/');

        if (nodeList.Length == 1)
        {
            if (nodeList[0] != E_RedDotDefine.rdRoot)
            {
                GF.LogInfo("Get Wrong RootNod！ current is " + nodeList[0]);
                return;
            }
        }

        //遍历子红点
        RedDotNode node = mRootNode;
        for (int i = 1; i < nodeList.Length; i++)
        {
            //父红点的 子红点字典表 内，必须包含
            if (node.rdChildrenDic.ContainsKey(nodeList[i]))
            {
                node = node.rdChildrenDic[nodeList[i]];

                //设置叶子红点的红点数
                if (i == nodeList.Length - 1)
                {
                    node.SetRedDotCount(Math.Max(0, rdCount));
                }
            }
            else
            {
                GF.LogError($"{node.rdName}的子红点字典内无 Key={nodeList[i]}, 检查 RedDotManager.InitRedDotTreeNode()");
                return;
            }
        }
    }

    /// <summary>
    /// 获取红点的计数
    /// </summary>
    /// <param name="nodePath"></param>
    /// <returns></returns>
    public int GetRedDotCount(string nodePath)
    {
        string[] nodeList = nodePath.Split('/');

        int count = 0;
        if (nodeList.Length >= 1)
        {
            //遍历子红点
            RedDotNode node = mRootNode;
            for (int i = 1; i < nodeList.Length; i++)
            {
                //父红点的 子红点字典表 内，必须包含
                if (node.rdChildrenDic.ContainsKey(nodeList[i]))
                {
                    node = node.rdChildrenDic[nodeList[i]];

                    if (i == nodeList.Length - 1)
                    {
                        count = node.rdCount;
                        break;
                    }
                }
            }
        }

        return count;
    }

    private void RefreshNodeRecursive(RedDotNode node)
    {
        // 调用节点的所有回调函数
        foreach (var callback in node.countChangeFuncs)
        {
            callback?.Invoke(node);
        }
        // 递归遍历子节点
        foreach (var child in node.rdChildrenDic.Values)
        {
            RefreshNodeRecursive(child);
        }
    }

    /// <summary>
    /// 全局刷新红点树
    /// </summary>
    public void RefreshAll()
    {
        if (mRootNode == null)
        {
            GF.LogError("RedDotManager: 根节点未初始化！");
            return;
        }

        RefreshNodeRecursive(mRootNode);
        GF.LogInfo("红点树已全局刷新！");
    }

    #endregion
}
