
using UnityEngine.UI;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class InfoPanel : UIFormBase
{
    public Text idTxt;
    public Text goldTxt;
    public Text dimTxt;

    public AvatarInfo avatarinfo;

    public Button BtnEditor;
    public RawImage avatar;

    public void OnEnable()
    {
        GF.LogInfo("InfoPanel  OnEnable");
        UpdatePanel();
    }

    public void UpdatePanel(){
        avatarinfo.Init(Util.GetMyselfInfo());
        idTxt.text = "ID:  " + Util.GetMyselfInfo().PlayerId.ToString();
        goldTxt.text = Util.GetMyselfInfo().Gold;
        dimTxt.text = Util.GetMyselfInfo().Diamonds.ToString();
    }

    public void Init()
    {
        GF.LogInfo("初始化个人信息");
        UpdatePanel();
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
    }

    public void Clear() {
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
    }

    protected override async void OnButtonClick(object sender, string btId)
    {
        switch (btId)
        {
            case "编辑资料":
                OnBtnEditorClick();
                break;
            case "钱包":
                await GF.UI.OpenUIFormAwait(UIViews.WalletPanel);
                break;
            case "牌谱":
                await GF.UI.OpenUIFormAwait(UIViews.MyPaipu);
                break;
            case "战绩":
                await GF.UI.OpenUIFormAwait(UIViews.GameRecord);
                break;
            case "反馈":
                await GF.UI.OpenUIFormAwait(UIViews.FeedbackDialog);
                break;
            case "客服":
                //如果当前是超盟创建者或者盟主创建者，则可以打开客服界面 否则弹窗请升级代理权限
                var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                if (leagueInfo != null && (leagueInfo.Type == 1 || leagueInfo.Type == 2) && leagueInfo.Creator == Util.GetMyselfInfo().PlayerId)
                {
                    await GF.UI.OpenUIFormAwait(UIViews.CustomSupportDialog);
                }
                else
                {
                    GF.UI.ShowToast("请升级代理权限");
                }
                break;
            case "设置":
                await GF.UI.OpenUIFormAwait(UIViews.SetPanel);
                break;
            case "商城":
                await GF.UI.OpenUIFormAwait(UIViews.ShopPanel);
                break;
        }
    }

    private async void OnBtnEditorClick()
    {
        GF.LogInfo("打开编辑个人界面");
        await GF.UI.OpenUIFormAwait(UIViews.EditorInfoPanel);
    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.Nick:
                avatarinfo.Init(Util.GetMyselfInfo());
                GF.LogInfo("收到 改名事件");
                break;
            case UserDataType.MONEY:
                goldTxt.text = Util.GetMyselfInfo().Gold.ToString();
                GF.LogInfo("收到 金币变动事件." , Util.GetMyselfInfo().Gold);
                break;
            case UserDataType.DIAMOND:
                dimTxt.text = Util.GetMyselfInfo().Diamonds.ToString();
                GF.LogInfo("收到 钻石变动事件." , Util.GetMyselfInfo().Diamonds.ToString());
                break;
            case UserDataType.VipState:
                avatarinfo.Init(Util.GetMyselfInfo());
                break;
            case UserDataType.avatar:
                Util.DownloadHeadImage(avatar, Util.GetMyselfInfo().HeadIndex);
                GF.LogInfo("收到 修改头像事件");
                break;
        }
    }

}
