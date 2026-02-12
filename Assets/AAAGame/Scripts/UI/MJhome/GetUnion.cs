using System.Collections;
using System.Collections.Generic;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class GetUnion : UIFormBase
{
    public InputField InputNum;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_UserApplyClubCapitalRs, Function_UserApplyClubCapitalRs);
    }
    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "确定":
                // 确定按钮被点击
                GF.LogInfo_wl("确定按钮被点击");
                Send_UserApplyClubGoldRq();
                break;
        }
    }
    public void Send_UserApplyClubGoldRq()
    {
        Msg_UserApplyClubCapitalRq req = MessagePool.Instance.Fetch<Msg_UserApplyClubCapitalRq>();
        long.TryParse(InputNum.text, out long num);
        if (num == 0)
            return;
        req.Amount = num;
        req.Option = 0;
        req.ClubId = GlobalManager.GetInstance().LeagueInfo.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_UserApplyClubCapitalRq, req);
    }
    /// <summary>
    ///  设置俱乐部额度返回
    /// </summary>
    /// <param name="data"></param>
    private void Function_UserApplyClubCapitalRs(MessageRecvData data)
    {
        // Msg_UserApplyClubCapitalRs ack = Msg_UserApplyClubCapitalRs.Parser.ParseFrom(data.Data);
        // GF.LogInfo("收到申请联盟币返回:" , ack.ToString());
        GF.UI.ShowToast("申请成功,等待审核");
        GF.UI.Close(this.UIForm);
    }
     protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_UserApplyClubCapitalRs, Function_UserApplyClubCapitalRs);
        base.OnClose(isShutdown, userData);
    }

}
