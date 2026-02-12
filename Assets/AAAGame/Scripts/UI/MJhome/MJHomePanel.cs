/// <summary>
/// 麻将大厅面板
/// </summary>
using UnityEngine.UI;
using GameFramework.Event;
using System;
using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MJHomePanel : UIFormBase
{
    public GameObject RollingBgContent;
    public RawImage avatar;

    private float lastScreenWidth;
    private float lastScreenHeight;

    /// <summary>
    /// 初始化面板
    /// </summary>
    /// <param name="userData">用户数据</param>
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        GF.LogInfo_wl("工会数量" + GlobalManager.GetInstance().MyJoinLeagueInfos.Count);
        varJiaruqinyouquan.SetActive(GlobalManager.GetInstance().MyJoinLeagueInfos.Count == 0);
        varKuaisujinru.SetActive(GlobalManager.GetInstance().MyJoinLeagueInfos.Count > 0);
        
    }

    void Update()
    {
        CheckAndHideChildrenAboveBoundary();
        if (Math.Abs(Screen.width - lastScreenWidth) > 0.1f || Math.Abs(Screen.height - lastScreenHeight) > 0.1f)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            RefreshMainBgScale();
        }
    }
    /// <summary>
    /// 打开面板时调用
    /// </summary>
    /// <param name="userData">用户数据</param>
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.LogInfo_wl("进入主界面");
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

        if (GlobalManager.GetInstance().LeagueInfo == null)
        {
            if (GlobalManager.GetInstance().MyLeagueInfos.Count > 0)
            {
                GlobalManager.GetInstance().LeagueInfo = GlobalManager.GetInstance().MyLeagueInfos[0];
            }
            else
            {
                if (GlobalManager.GetInstance().MyJoinLeagueInfos.Count > 0)
                {
                    foreach (var info in GlobalManager.GetInstance().MyJoinLeagueInfos)
                    {
                        GlobalManager.GetInstance().LeagueInfo = info.Value;
                        GF.LogInfo_wl("[MJHomePanel] 设置默认亲友圈为参与的第一个");
                        break;
                    }
                }
            }
        }
        else
        {
        varQYQname.text = GlobalManager.GetInstance().LeagueInfo.LeagueName;
        }
        varJiaruqinyouquan.SetActive(GlobalManager.GetInstance().LeagueInfo == null);
        varKuaisujinru.SetActive(GlobalManager.GetInstance().LeagueInfo != null);
        varPanelTop.transform.Find("name").GetComponent<Text>().text = Util.GetMyselfInfo().NickName;
        varPanelTop.transform.Find("id").GetComponent<Text>().text = "ID:" + Util.GetMyselfInfo().PlayerId;
        //设置头像
        Util.DownloadHeadImage(avatar, Util.GetMyselfInfo().HeadIndex, -1);
        //需要自适应MainBg的宽度和高度缩放
        RefreshMainBgScale();
    }
    private void RefreshMainBgScale()
    {
        Transform mainBg = transform.Find("MainSpine/MainBg");
        if (mainBg != null)
        {
            // 背景Spine设计尺寸（在scale=100时的显示尺寸）
            const float BGDesignWidth = 2340f;
            const float BGDesignHeight = 1080f;

            // 关键：必须基于CanvasScaler的缩放结果来算“逻辑尺寸”，否则会出现你说的
            // 2480x2200 计算出 106x204（按屏幕算）但实际应该接近 82x156（按Canvas逻辑算）的问题。
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float screenAspect = screenWidth / screenHeight;

            Canvas canvas = null;
            var rootCanvas = GFBuiltin.RootCanvas;
            if (rootCanvas != null)
            {
                canvas = rootCanvas;
            }
            else
            {
                canvas = GetComponentInParent<Canvas>();
            }

            CanvasScaler canvasScaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;

            // 默认兜底：如果拿不到CanvasScaler，就退回到屏幕计算（但这通常会和实际UI不一致）
            float logicalWidth = screenWidth;
            float logicalHeight = screenHeight;
            float match = -1f;
            Vector2 referenceResolution = Vector2.zero;

            if (canvasScaler != null
                && canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                && canvasScaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                referenceResolution = canvasScaler.referenceResolution;
                match = canvasScaler.matchWidthOrHeight;

                // Unity MatchWidthOrHeight 的缩放因子：
                // match=0 -> width:  scale = screenW / refW
                // match=1 -> height: scale = screenH / refH
                // 0~1 之间用 log 插值
                float logWidth = Mathf.Log(screenWidth / referenceResolution.x, 2f);
                float logHeight = Mathf.Log(screenHeight / referenceResolution.y, 2f);
                float logWeighted = Mathf.Lerp(logWidth, logHeight, match);
                float scaleFactor = Mathf.Pow(2f, logWeighted);

                // 反推出“Canvas逻辑尺寸”（即在referenceResolution坐标系下，屏幕等价多少宽高）
                logicalWidth = screenWidth / scaleFactor;
                logicalHeight = screenHeight / scaleFactor;
            }

            // 用“逻辑尺寸”对比背景设计尺寸，得到需要的缩放（scale=100时对应设计尺寸）
            float scaleX = (logicalWidth / BGDesignWidth) * 100f;
            float scaleY = (logicalHeight / BGDesignHeight) * 100f;

            // 规则：
            // 1) 两个方向都不超（<=100） -> 保持100
            // 2) 只超一个方向（一个>100，一个<100）-> 非等比（避免等比放大太大）
            // 3) 两个方向都超（都>100） -> 等比（避免变形）
            if (scaleX <= 100f && scaleY <= 100f)
            {
                mainBg.localScale = new Vector3(100f, 100f, 1f);
                GF.LogInfo($"UIBgAdapter[保持100]: Screen={screenWidth}x{screenHeight}({screenAspect:F3}), Logical={logicalWidth:F0}x{logicalHeight:F0}, match={match:F2}, ref={referenceResolution}");
                return;
            }

            bool onlyOneAxisOver = (scaleX > 100f && scaleY < 100f) || (scaleY > 100f && scaleX < 100f);
            if (onlyOneAxisOver)
            {
                // 典型：折叠屏/平板接近正方形，match=0 导致 LogicalHeight 很大
                // 这时应该接近你手动调的 82x156，而不是等比到 156x156
                mainBg.localScale = new Vector3(scaleX, scaleY, 1f);
                GF.LogInfo($"UIBgAdapter[非等比]: Screen={screenWidth}x{screenHeight}({screenAspect:F3}), Logical={logicalWidth:F0}x{logicalHeight:F0}, match={match:F2}, ref={referenceResolution}, scaleX={scaleX:F2}, scaleY={scaleY:F2}");
            }
            else
            {
                float scale = Mathf.Max(scaleX, scaleY);
                mainBg.localScale = new Vector3(scale, scale, 1f);
                GF.LogInfo($"UIBgAdapter[等比]: Screen={screenWidth}x{screenHeight}({screenAspect:F3}), Logical={logicalWidth:F0}x{logicalHeight:F0}, match={match:F2}, ref={referenceResolution}, scaleX={scaleX:F2}, scaleY={scaleY:F2}, final={scale:F2}");
            }
        }
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueSwitch:
                // 俱乐部切换时更新UI
                var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                varJiaruqinyouquan.SetActive(leagueInfo == null);
                varKuaisujinru.SetActive(leagueInfo != null);
                if (leagueInfo != null)
                {
                    varQYQname.text = leagueInfo.LeagueName;
                    Util.DownloadHeadImage(avatar, leagueInfo.LeagueHead, -1);
                }
                break;
            case ClubDataType.eve_LeagueDateUpdate:
                // 俱乐部信息更新
                SetUI();
                break;
        }
    }
    /// <summary>
    /// 关闭面板时调用
    /// </summary>
    /// <param name="isShutdown">是否为关闭游戏</param>
    /// <param name="userData">用户数据</param>
    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

    }

    /// <summary>
    /// 点击关闭按钮时调用
    /// </summary>
    public override void OnClickClose()
    {
        if (Util.IsClickLocked()) return;
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        Util.GetInstance().OpenMJConfirmationDialog("退出麻将大厅", "确定要退回到大厅吗?", () =>
        {
            if (GF.Procedure.CurrentProcedure is MJHomeProcedure mjHomeProcedure)
            {
                mjHomeProcedure.ExitMJHome();
            }
        });
    }
    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);

        switch (btId)
        {
            case "创建房间":
                // 打开创建房间界面，设置为创建房间模式
                var uiParams = UIParams.Create();
                uiParams.Set<VarString>("setFloor", "CreateRoom");
                await GF.UI.OpenUIFormAwait(UIViews.CreatMjPopup, uiParams);
                break;
            case "加入亲友圈":
                ShowJoinFriendCircle(0); // 使用索引0显示亲友圈图片
                break;
            case "快速进入":
                await GF.UI.OpenUIFormAwait(UIViews.CreatMJRoom);
                break;
            case "加入房间":
                ShowJoinFriendCircle(1); // 使用索引1显示房间图片
                break;
            case "更多亲友圈":
                await GF.UI.OpenUIFormAwait(UIViews.MoreQyq);
                break;
            //
            case "更多游戏":
                break;
            case "编辑资料":
                OnBtnEditorClick();
                break;
            case "战绩":
                await GF.UI.OpenUIFormAwait(UIViews.MJGameRecord);
                break;
            case "商城":
                var shop = await GF.UI.OpenUIFormAwait(UIViews.MJShopPanel);
                shop.GetComponent<MJShopPanel>().varShopContent.transform.Find("钻石").GetComponent<Toggle>().isOn = true;
                break;
            // case "反馈":
            //     await GF.UI.OpenUIFormAwait(UIViews.FeedbackDialog);
            //     break;
            case "客服":
            var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                if (leagueInfo != null && (leagueInfo.Type == 1 || leagueInfo.Type == 2) && leagueInfo.Creator == Util.GetMyselfInfo().PlayerId)
                {
                    await GF.UI.OpenUIFormAwait(UIViews.MJCustomerService);
                }
                else
                {
                    GF.UI.ShowToast("请升级代理权限");
                }
                break;
            case "敬请期待":
                GF.UI.ShowToast("敬请期待");
                break;

                // case "设置":
                //     await GF.UI.OpenUIFormAwait(UIViews.SetPanel);
                //     break;
        }
    }
    private async void OnBtnEditorClick()
    {
        GF.LogInfo("打开编辑个人界面");
        await GF.UI.OpenUIFormAwait(UIViews.EditorInfoPanel);
    }
    public async void ShowJoinFriendCircle(int imageIndex)
    {
        var joinFriendCircleForm = await GF.UI.OpenUIFormAwait(UIViews.JoinFriendCircle);
        //根据索引来改变这个预制体中的图片
        if (joinFriendCircleForm != null)
        {
            // 获取JoinFriendCircle组件并调用ChangeTopImage方法
            var joinFriendCircle = joinFriendCircleForm.GetComponent<JoinFriendCircle>();
            if (joinFriendCircle != null)
            {
                joinFriendCircle.ChangeTopImage(imageIndex);
            }
        }
    }
    public void SetUI()
    {
        varJiaruqinyouquan.SetActive(GlobalManager.GetInstance().MyJoinLeagueInfos.Count == 0);
        varKuaisujinru.SetActive(GlobalManager.GetInstance().MyJoinLeagueInfos.Count > 0);
    }

    /// <summary>
    /// 检查RollingBgContent下的子物体，当子物体超过上方边界一半时隐藏
    /// </summary>
    private void CheckAndHideChildrenAboveBoundary()
    {
        if (RollingBgContent == null) return;

        // 获取RollingBgContent的RectTransform
        RectTransform parentRect = RollingBgContent.GetComponent<RectTransform>();
        if (parentRect == null) return;

        // 获取父物体的ScrollRect（如果有的话，通常Grid Layout会在ScrollRect中）
        Transform scrollViewParent = parentRect.parent;
        RectTransform viewportRect = scrollViewParent != null ? scrollViewParent.GetComponent<RectTransform>() : null;

        // 如果有viewport，使用viewport的上边界，否则使用父物体自身的上边界
        float topBoundary;
        if (viewportRect != null)
        {
            Vector3[] viewportCorners = new Vector3[4];
            viewportRect.GetWorldCorners(viewportCorners);
            topBoundary = viewportCorners[1].y; // viewport左上角的y坐标
        }
        else
        {
            Vector3[] parentCorners = new Vector3[4];
            parentRect.GetWorldCorners(parentCorners);
            topBoundary = parentCorners[1].y; // 父物体左上角的y坐标
        }

        // 遍历所有子物体
        for (int i = 0; i < RollingBgContent.transform.childCount; i++)
        {
            Transform child = RollingBgContent.transform.GetChild(i);
            RectTransform childRect = child.GetComponent<RectTransform>();

            if (childRect == null) continue;

            // 获取子物体的世界坐标边界
            Vector3[] childCorners = new Vector3[4];
            childRect.GetWorldCorners(childCorners);

            // 子物体的顶部和底部y坐标
            float childTop = childCorners[1].y;
            float childBottom = childCorners[0].y;
            float childHeight = childTop - childBottom;

            // 计算子物体中心点y坐标
            float childCenterY = childBottom + childHeight / 2f;

            // 如果子物体中心点超过上方边界（即超过一半），则隐藏
            if (childCenterY > topBoundary)
            {

                var firstChild = child.gameObject.transform.GetChild(0).gameObject;
                firstChild.SetActive(false);

            }
            else
            {
                var firstChild = child.gameObject.transform.GetChild(0).gameObject;
                firstChild.SetActive(true);

            }
        }
    }

}