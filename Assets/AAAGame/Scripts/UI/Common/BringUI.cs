using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;



public class BringUI : MonoBehaviour
{
    public Text dairu;
    public Slider slider;
    public Button BtnOK;
    public Button BtnClose;

    public GameObject midObj;

    public float Min = 10;
    private List<float> kedu = new List<float>();

    public Text textZichan;
    public Text textExpend;

    private long bringClubId = 0;
    NNProcedure nnProcedure;
    DZProcedure dzProcedure;
    ZJHProcedure zjhProcedure;
    BJProcedure bjProcedure;

    public void Clear(){
        HideBring();
    }

    bool leave = false;
    int bringNum;

    public void InitDate(bool leave) {
        slider.value = 0;
        dairu.text = Min.ToString();
        this.leave = leave;
        if (nnProcedure != null) {
            bringNum = nnProcedure.GetSelfPlayer() == null ? 0 : nnProcedure.GetSelfPlayer().BringNum;
            midObj.SetActive(nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskType == DeskType.League || nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskType == DeskType.Super);
            if (nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskType == DeskType.League || nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskType == DeskType.Super)
            {
                Util.UpdateClubInfoRq();
                SetDate(nnProcedure.enterNiuNiuDeskRs.BaseConfig.MinBringIn, 0f);
            }
            else
            {
                SetDate(nnProcedure.enterNiuNiuDeskRs.BaseConfig.MinBringIn, float.Parse(Util.GetMyselfInfo().Gold));
            }
        }
        if (dzProcedure != null) {
            bringNum = dzProcedure.GetSelfPlayer() == null ? 0 : dzProcedure.GetSelfPlayer().BringNum;
            midObj.SetActive(dzProcedure.deskinfo.BaseConfig.DeskType == DeskType.League || dzProcedure.deskinfo.BaseConfig.DeskType == DeskType.Super);
            if (dzProcedure.deskinfo.BaseConfig.DeskType == DeskType.League || dzProcedure.deskinfo.BaseConfig.DeskType == DeskType.Super)
            {
                Util.UpdateClubInfoRq();
                SetDate(dzProcedure.deskinfo.BaseConfig.MinBringIn, 0f);
            }
            else
            {
                SetDate(dzProcedure.deskinfo.BaseConfig.MinBringIn, float.Parse(Util.GetMyselfInfo().Gold));
            }
        }
        if (zjhProcedure != null) {
            bringNum = zjhProcedure.GetSelfPlayer() == null ? 0 : zjhProcedure.GetSelfPlayer().BringNum;
            midObj.SetActive(zjhProcedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.League || zjhProcedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Super);
            if (zjhProcedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.League || zjhProcedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Super)
            {
                Util.UpdateClubInfoRq();
                SetDate(zjhProcedure.enterZjhDeskRs.BaseConfig.MinBringIn, 0f);
            }
            else
            {
                SetDate(zjhProcedure.enterZjhDeskRs.BaseConfig.MinBringIn, float.Parse(Util.GetMyselfInfo().Gold));
            }
        }
        if (bjProcedure != null) {
            bringNum = bjProcedure.GetSelfPlayer() == null ? 0 : bjProcedure.GetSelfPlayer().BringNum;
            midObj.SetActive(bjProcedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.League || bjProcedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Super);
            if (bjProcedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.League || bjProcedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Super)
            {
                Util.UpdateClubInfoRq();
                SetDate(bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.MinBringIn, 0f);
            }
            else
            {
                SetDate(bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.MinBringIn, float.Parse(Util.GetMyselfInfo().Gold));
            }
        }
    }

    public void SetDate(int min, float max)
    {
        Min = min;
        dairu.text = Min.ToString();
        kedu.Clear();
        if (max >= min) {
            for (int i = 1;i <= 10;i++) {
                if (max > min * i) {
                    kedu.Add(min * i);
                }
                else if (max == min * i) {
                    kedu.Add(min * i);
                    break;
                }
                else {
                    kedu.Add(max);
                    break;
                }
            }
            slider.maxValue = kedu.Count - 1;
        }
        else {
            for (int i = 1;i <= 10;i++) {
                kedu.Add(min * i);
            }
            slider.maxValue = 9;
        }

        slider.value = 0;
        textZichan.text = "总资产：" + Util.FormatAmount(max);
        textExpend.text = $"带入费：<color=#FF0000>{(bringNum > 0 ? GlobalManager.GetConstants(1) : "首次免费")}</color>";
    }

    /// <summary>
    /// 我的公会列表
    /// </summary>
    /// <param name="data"></param>
    private void FunckMyLeagueListRs(MessageRecvData data)
    {
        if (gameObject.activeSelf == false)
        {
            GF.LogInfo("BringUI不活跃，忽略处理联盟列表消息");
            return;
        }
        Msg_MyLeagueListRs ack = Msg_MyLeagueListRs.Parser.ParseFrom(data.Data);
        if (ack.Infos.Count != 0)
        {
            ShowMyLeagueList(ack);
        }
    }


    //显示工会列表
    public GameObject content;
    public GameObject itemPrefab;
    public void ShowMyLeagueList(Msg_MyLeagueListRs ack)
    {
        bool isInit = false;
        //创建列表
        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }
        var Infos = ack.Infos.ToArray();
        DeskPlayer deskplayer = null;
        if (nnProcedure != null) {
            deskplayer = nnProcedure.GetSelfPlayer();
        }
        if (dzProcedure != null) {
            deskplayer = dzProcedure.GetSelfPlayer();
        }
        if (zjhProcedure != null) {
            deskplayer = zjhProcedure.GetSelfPlayer();
        }
        if (bjProcedure != null) {
            deskplayer = bjProcedure.GetSelfPlayer();
        }
        if (deskplayer == null) {
            return;
        }
        for (int i = 0; i < Infos.Length; i++)
        {
            var data = Infos[i];
            //忽略联盟(超级联盟)和不是这个联盟的俱乐部
            if (data.Type == 1 || data.Type == 2) {
                continue;
            }
            if (nnProcedure != null)
            {
                if (nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskType == DeskType.League)
                {
                    if (data.Father != nnProcedure.enterNiuNiuDeskRs.BaseConfig.ClubId)
                    {
                        continue;
                    }
                }else if(nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskType == DeskType.Super){
                    if (data.FatherInfo == null || data.FatherInfo.Father != nnProcedure.enterNiuNiuDeskRs.BaseConfig.ClubId)
                    {
                        continue;
                    }
                }
            }
            if (dzProcedure != null && data.Father != dzProcedure.deskinfo.BaseConfig.ClubId)
            {
                if (dzProcedure.deskinfo.BaseConfig.DeskType == DeskType.League)
                {
                    if (data.Father != dzProcedure.deskinfo.BaseConfig.ClubId)
                    {
                        continue;
                    }
                }else if(dzProcedure.deskinfo.BaseConfig.DeskType == DeskType.Super){
                    if (data.FatherInfo == null || data.FatherInfo.Father != dzProcedure.deskinfo.BaseConfig.ClubId)
                    {
                        continue;
                    }
                }
            }
            if (zjhProcedure != null && data.Father != zjhProcedure.enterZjhDeskRs.BaseConfig.ClubId)
            {
                if (zjhProcedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.League)
                {
                    if (data.Father != zjhProcedure.enterZjhDeskRs.BaseConfig.ClubId)
                    {
                        continue;
                    }
                }else if(zjhProcedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Super){
                    if (data.FatherInfo == null || data.FatherInfo.Father != zjhProcedure.enterZjhDeskRs.BaseConfig.ClubId)
                    {
                        continue;
                    }
                }
            }
            if (bjProcedure != null && data.Father != bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.ClubId)
            {
                if (bjProcedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.League)
                {
                    if (data.Father != bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.ClubId)
                    {
                        continue;
                    }
                }else if(bjProcedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Super){
                    if (data.FatherInfo == null || data.FatherInfo.Father != bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.ClubId)
                    {
                        continue;
                    }
                }
            }
            if (deskplayer != null && deskplayer.ClubId != 0 && deskplayer.ClubId != data.LeagueId) {
                continue;
            }
            GameObject newItem = Instantiate(itemPrefab);
            newItem.transform.SetParent(content.transform, false);
            newItem.transform.Find("Nickname").GetComponent<Text>().text = data.LeagueName;
            newItem.transform.Find("Toggle").GetComponent<Toggle>().group = content.GetComponent<ToggleGroup>();
            //如果没有选择俱乐部 默认第一个
            if ((deskplayer.ClubId == 0 && !isInit) || deskplayer.ClubId == data.LeagueId)
            {
                isInit = true;
                newItem.transform.Find("Toggle").GetComponent<Toggle>().isOn = true;
                ChooseClubChange(true, data);
            }
            newItem.transform.Find("Toggle").GetComponent<Toggle>().onValueChanged.AddListener(isOn => ChooseClubChange(isOn, data));
        }
    }

    private void ChooseClubChange(bool isOn, LeagueInfo data)
    {
        if (isOn){
            bringClubId = data.LeagueId;
            float gold = GlobalManager.GetInstance().GetAllianceCoins(data.LeagueId);
            if (nnProcedure != null) {
                SetDate(nnProcedure.enterNiuNiuDeskRs.BaseConfig.MinBringIn, Mathf.Floor(gold));
            }
            if (dzProcedure != null) {
                SetDate(dzProcedure.deskinfo.BaseConfig.MinBringIn, Mathf.Floor(gold));
            }
            if (zjhProcedure != null) {
                SetDate(zjhProcedure.enterZjhDeskRs.BaseConfig.MinBringIn, Mathf.Floor(gold));
            }
            if (bjProcedure != null) {
                SetDate(bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.MinBringIn, Mathf.Floor(gold));
            }
        } 
    }

    public void OnSliderValueChanged()
    {
        // 这里处理滑块值变化的逻辑
        int dai = (int)slider.value;
        dairu.text = kedu[dai].ToString();
    }
    public void Dairu()
    {
        if (nnProcedure != null) {
            if ((nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskType == DeskType.League || nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskType == DeskType.Super) && bringClubId == 0)
            {
                GF.UI.ShowToast("请选择俱乐部!");
                return;
            }
            else {
                nnProcedure.Send_BringRq(int.Parse(dairu.text), bringClubId);
            }
        }
        if (dzProcedure != null) {
            if ((dzProcedure.deskinfo.BaseConfig.DeskType == DeskType.League || dzProcedure.deskinfo.BaseConfig.DeskType == DeskType.Super) && bringClubId == 0)
            {
                GF.UI.ShowToast("请选择俱乐部!");
                return;
            }
            else {
                dzProcedure.Send_BringRq(int.Parse(dairu.text), bringClubId);
            }
        }
        if (zjhProcedure != null) {
            if ((zjhProcedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.League || zjhProcedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Super) && bringClubId == 0)
            {
                GF.UI.ShowToast("请选择俱乐部!");
                return;
            }
            else {
                zjhProcedure.Send_BringRq(int.Parse(dairu.text), bringClubId);
            }
        }
        if (bjProcedure != null) {
            if ((bjProcedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.League || bjProcedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Super) && bringClubId == 0)
            {
                GF.UI.ShowToast("请选择俱乐部!");
                return;
            }
            else {
                bjProcedure.Send_BringRq(int.Parse(dairu.text), bringClubId);
            }
        }
        HideBring();
    }

    public void ShowBring(bool leave)
    {
        GF.LogInfo("ShowBring");
        if (GF.Procedure.CurrentProcedure is NNProcedure)
        {
            nnProcedure = GF.Procedure.CurrentProcedure as NNProcedure;
        }
        if (GF.Procedure.CurrentProcedure is DZProcedure)
        {
            dzProcedure = GF.Procedure.CurrentProcedure as DZProcedure;
        }
        if (GF.Procedure.CurrentProcedure is ZJHProcedure)
        {
            zjhProcedure = GF.Procedure.CurrentProcedure as ZJHProcedure;
        }
        if (GF.Procedure.CurrentProcedure is BJProcedure)
        {
            bjProcedure = GF.Procedure.CurrentProcedure as BJProcedure;
        }
        HotfixNetworkComponent.AddListener(MessageID.Msg_MyLeagueListRs, FunckMyLeagueListRs);
        transform.GetComponent<CanvasGroup>().alpha = 0;
        transform.GetComponent<CanvasGroup>().DOFade(1, 0.2f).SetEase(Ease.InOutFlash);
        gameObject.SetActive(true);
        InitDate(leave);
    }

    public void HideBring(){
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MyLeagueListRs, FunckMyLeagueListRs);
        transform.GetComponent<CanvasGroup>().alpha = 1;
        transform.GetComponent<CanvasGroup>().DOFade(0, 0.2f).SetEase(Ease.InOutFlash);
        gameObject.SetActive(false);
    }

    public void Close()
    {
		//关闭了，判断当前带入是否是0
		//先判断是否坐下了
		// if (nnProcedure.nnGamePanel.seatManager.GetSelfSeat() != null)
		// {
		//     SeatNode seatNode = nnProcedure.nnGamePanel.seatManager.GetSelfSeat();
		//     float Coin = 10000f;
		//     float.TryParse(seatNode.Player.deskPlayer.Coin, out Coin);
		//     if (Coin <= 0)
		//     {
		//         //发送站起
		//         nnProcedure.Send_SitUpRq();
		//     }
		// }
		DeskPlayer deskplayer;
		if (nnProcedure != null) {
            deskplayer = nnProcedure.GetSelfPlayer();
            if (deskplayer != null && deskplayer.Pos != Position.NoSit/* && deskplayer.InGame == false*/) {
                if (leave == false) {
                    if (float.Parse(deskplayer.Coin) <= 0 && deskplayer.State != PlayerState.WaitBring) {
                        nnProcedure.Send_SitUpRq();
                    }
                }
                else {
                    nnProcedure.Send_SitUpRq();
                }
            }
        }
        if (dzProcedure != null) {
            deskplayer = dzProcedure.GetSelfPlayer();
            if (deskplayer != null && deskplayer.Pos != Position.NoSit/* && deskplayer.InGame == false*/) {
                if (leave == false) {
                    if (float.Parse(deskplayer.Coin) <= 0 && deskplayer.State != PlayerState.WaitBring)
                    {
                        dzProcedure.Send_SitUpRq();
                    }
                }
                else {
                    dzProcedure.Send_SitUpRq();
                }
            }
        }
        if (zjhProcedure != null) {
            deskplayer = zjhProcedure.GetSelfPlayer();
            if (deskplayer != null && deskplayer.Pos != Position.NoSit/* && deskplayer.InGame == false*/) {
                if (leave == false) {
                    if (float.Parse(deskplayer.Coin) <= 0 && deskplayer.State != PlayerState.WaitBring)
                    {
                        zjhProcedure.Send_SitUpRq();
                    }
                }
                else {
                    zjhProcedure.Send_SitUpRq();
                }
            }
        }
        if (bjProcedure != null) {
            deskplayer = bjProcedure.GetSelfPlayer();
            if (deskplayer != null && deskplayer.Pos != Position.NoSit/* && deskplayer.InGame == false*/) {
                if (leave == false) {
                    if (float.Parse(deskplayer.Coin) <= 0 && deskplayer.State != PlayerState.WaitBring)
                    {
                        bjProcedure.Send_SitUpRq();
                    }
                }
                else {
                    bjProcedure.Send_SitUpRq();
                }
            }
        }
        HideBring();
    }
}
