using System.Collections.Generic;
using NetMsg;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MJHallSet : UIFormBase

{

    //当前请求的楼层数组
    private List<long> floorList = new List<long>();
    //当前添加的楼层还未请求的楼层数组
    private List<long> floorListItem = new List<long>();
    //备份：用于未保存时恢复
    private List<long> backupFloorListItem = new List<long>();

    private string MixHallName = "大厅1"; //限制1-4个字
    private bool isHallOpen = false;
    private int CurrentOpenHall = 0;//当前打开的大厅
    //
    public List<Sprite> floorNumRawImage = new List<Sprite>();

    // 可选上下文：非静态数据源
    private global::HallContext _ctx;

    /// <summary>
    /// 获取正确的ClubId - 根据上级关系决定使用哪个ClubId
    /// </summary>
    /// <returns>正确的ClubId</returns>
    private long GetCorrectClubId()
    {
        var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
        
        if (leagueInfo.Father == 0)
        {
            return leagueInfo.LeagueId;
        }
        else if (leagueInfo.Father != 0 && leagueInfo.FatherInfo.Father == 0)
        {
            return leagueInfo.Father;
        }
        else if (leagueInfo.Father != 0 && leagueInfo.FatherInfo.Father != 0)
        {
            return leagueInfo.FatherInfo.Father;
        }
        
        return leagueInfo.LeagueId; // 默认返回当前联盟ID
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        _ctx = userData as global::HallContext;
        varMix_AddFloor.SetActive(false);
        if (_ctx != null && _ctx.Halls != null && _ctx.Halls.Count > 0)
        {
            CurrentOpenHall = Mathf.Clamp(_ctx.CurrentHallIndex, 0, _ctx.Halls.Count - 1);
            floorList = new List<long>(_ctx.Halls[CurrentOpenHall].Floor);
        }
        else
        {
            floorList = new List<long>();
        }
    }
    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "保存混排大厅设置":
                //   int64 clubId = 1;
                //   string hallName = 2;//大厅名称
                //   repeated int64 floor = 3;//楼层
                //   bool hallOpen = 4;//大厅启用状态
                isHallOpen = varIsOpenHall.transform.GetComponent<Toggle>().isOn;
                if (floorList.Count == 0 && isHallOpen)
                {
                    GF.UI.ShowToast("请添加楼层");
                    return;
                }
                Msg_SetHallFloorRq req = MessagePool.Instance.Fetch<Msg_SetHallFloorRq>();
                req.ClubId = GetCorrectClubId();
                req.HallName = MixHallName;
                req.Floor.AddRange(floorList);
                req.HallOpen = isHallOpen;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                      (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetHallFloorRq, req);
                      //关闭界面
                    GF.UI.Close(this.UIForm);
                break;
                
            case "保存混排楼层设置":
                //关闭界面

                varMix_AddFloor.SetActive(false);
                floorList = floorListItem;
                // 构建一个本地方法用于重建混排大厅UI并绑定移动按钮
                void RebuildMixHallUI()
                {
                    foreach (Transform ta in varMixHallContent.transform)
                    {
                        Destroy(ta.gameObject);
                    }

                    // 使用上下文的 Floors 作为全量配置来源（否则空列表）
                    List<Msg_Floor> allFloors = _ctx != null && _ctx.Floors != null ? _ctx.Floors : new List<Msg_Floor>();

                    for (int i = 0; i < floorList.Count; i++)
                    {
                        var go = Instantiate(varMixHallContentItem, varMixHallContent.transform);

                        int idx = allFloors.FindIndex(x => x.Floor == floorList[i]);
                        if (idx != -1)
                        {
                            go.transform.Find("FloorNum").GetComponent<Image>().sprite = floorNumRawImage[idx];
                            go.transform.Find("GameName").GetComponent<Text>().text = allFloors[idx].Config.DeskName;
                            go.transform.Find("GameNum").GetComponent<Text>().text = allFloors[idx].Config.PlayerNum
                             + "人" + allFloors[idx].Config.PlayerTime + "局";
                            go.transform.Find("FloorRemark/text").GetComponent<Text>().text = allFloors[idx].Config.DeskName;
                        }

                        Button moveUp = go.transform.Find("MoveConent/MoveUp").gameObject.GetComponent<Button>();
                        Button moveDown = go.transform.Find("MoveConent/MoveDown").gameObject.GetComponent<Button>();
                        Button moveTop = go.transform.Find("MoveConent/MoveTop").gameObject.GetComponent<Button>();

                        bool isFirst = i == 0;
                        bool isLast = i == floorList.Count - 1;
                        // 第一个只显示 MoveDown；最后一个只显示 MoveUp 和 MoveTop；中间三个都显示
                        moveUp.gameObject.SetActive(!isFirst);
                        moveDown.gameObject.SetActive(!isLast);
                        moveTop.gameObject.SetActive(!isFirst);

                        int indexLocal = i;
                        moveUp.onClick.RemoveAllListeners();
                        moveDown.onClick.RemoveAllListeners();
                        moveTop.onClick.RemoveAllListeners();

                        moveUp.onClick.AddListener(() =>
                        {
                            if (indexLocal > 0)
                            {
                                long t = floorList[indexLocal - 1];
                                floorList[indexLocal - 1] = floorList[indexLocal];
                                floorList[indexLocal] = t;
                                RebuildMixHallUI();
                            }
                        });

                        moveDown.onClick.AddListener(() =>
                        {
                            if (indexLocal < floorList.Count - 1)
                            {
                                long t = floorList[indexLocal + 1];
                                floorList[indexLocal + 1] = floorList[indexLocal];
                                floorList[indexLocal] = t;
                                RebuildMixHallUI();
                            }
                        });

                        moveTop.onClick.AddListener(() =>
                        {
                            if (indexLocal > 0)
                            {
                                long val = floorList[indexLocal];
                                floorList.RemoveAt(indexLocal);
                                floorList.Insert(0, val);
                                RebuildMixHallUI();
                            }
                        });
                    }
                }

                RebuildMixHallUI();
                break;
            case "点击添加混排楼层":
                // 进入编辑界面前备份当前工作列表，用于未保存时恢复
                backupFloorListItem = new List<long>(floorListItem);
                // 使用当前保存的楼层列表作为初始可编辑列表（克隆避免引用问题）
                floorListItem = new List<long>(floorList);
                varMix_AddFloor.SetActive(true);
                foreach (Transform ta in varAddFloorConent.transform)
                {
                    Destroy(ta.gameObject);
                }

                // 使用上下文的 Floors 作为全量配置来源（否则空列表）
                List<Msg_Floor> allFloorsForAdd = _ctx != null && _ctx.Floors != null ? _ctx.Floors : new List<Msg_Floor>();

                for (int i = 0; i < allFloorsForAdd.Count; i++)
                {
                    var go = Instantiate(varAddFloorConentItem, varAddFloorConent.transform);

                    var floorData = allFloorsForAdd[i];
                    var config = floorData.Config;

                    var floorNumImage = go.transform.Find("FloorNum").GetComponent<Image>();
                    var gameNameText = go.transform.Find("GameName").GetComponent<Text>();
                    var gameNumText = go.transform.Find("GameNum").GetComponent<Text>();
                    var remarkText = go.transform.Find("FloorRemark/text").GetComponent<Text>();

                    floorNumImage.sprite = floorNumRawImage[i];
                    gameNameText.text = config.DeskName;
                    gameNumText.text = config.PlayerNum + "人" + config.PlayerTime + "局";
                    remarkText.text = config.DeskName;

                    // 按钮与显示控制
                    var addBtn = go.transform.Find("AddBtn").gameObject.GetComponent<Button>();
                    var deleteBtn = go.transform.Find("AddBtn/DeleteBtn").gameObject.GetComponent<Button>();

                    bool isFloorInHall = floorListItem.Contains(floorData.Floor);
                    // 楼层已在大厅：隐藏添加按钮，显示删除按钮；反之亦然
                    addBtn.gameObject.SetActive(!isFloorInHall);
                    deleteBtn.gameObject.SetActive(isFloorInHall);

                    int index = i; // 捕获索引用于闭包
                    addBtn.onClick.RemoveAllListeners();
                    addBtn.onClick.AddListener(() =>
                    {
                        long floorId = allFloorsForAdd[index].Floor;
                        if (!floorListItem.Contains(floorId))
                        {
                            floorListItem.Add(floorId);
                        }
                        addBtn.gameObject.SetActive(false);
                        deleteBtn.gameObject.SetActive(true);
                    });

                    deleteBtn.onClick.RemoveAllListeners();
                    deleteBtn.onClick.AddListener(() =>
                    {
                        long floorId = allFloorsForAdd[index].Floor;
                        floorListItem.Remove(floorId);
                        addBtn.gameObject.SetActive(true);
                        deleteBtn.gameObject.SetActive(false);
                    });
                }
                //如果没点保存退出则要恢复之前状态
                break;
            case "关闭混排楼层设置":
                // 未点击保存就关闭：恢复进入编辑前的状态
                floorListItem = new List<long>(backupFloorListItem);
                varMix_AddFloor.SetActive(false);
                break;
        }
    }

}
