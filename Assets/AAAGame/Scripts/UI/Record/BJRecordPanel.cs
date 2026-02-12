using NetMsg;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class BJRecordPanel : UIFormBase
{
	Msg_CKPlayBackRs msg_playback = null;
	protected override void OnClose(bool isShutdown, object userData)
	{
		base.OnClose(isShutdown, userData);
	}

	protected override void OnOpen(object userData)
	{
		base.OnOpen(userData);
		msg_playback = Msg_CKPlayBackRs.Parser.ParseFrom(Params.Get<VarByteArray>("PlayBack"));
		//初始化座位
        varSeats.Init(msg_playback.Config.Config.PlayerNum);
        //初始化房间信息
        varRoomNameID.text = "房间:" + msg_playback.Config.Config.DeskName + "(ID:" + msg_playback.DeskId + ")";
        varRoominfo.text = "底注: " + msg_playback.Config.Config.BaseCoin +
							  "  王牌: " + (msg_playback.Config.CompareChickenConfig.King == 0 ? "无王牌" : msg_playback.Config.CompareChickenConfig.King + "张") +
							  "\n摆牌时间: " + msg_playback.Config.CompareChickenConfig.OptionTime +
							  (msg_playback.Config.Config.IpLimit && msg_playback.Config.Config.GpsLimit ? "\nIP/GPS限制" : 
                              (msg_playback.Config.Config.IpLimit ? "\nIP限制" : "") +
                              (msg_playback.Config.Config.GpsLimit ? "\nGPS限制" : ""));
        //初始化座位玩家信息
        for (int i = 0; i < msg_playback.DeskPlayer.Count; i++)
        {
            varSeats.PlayerEnter(msg_playback.DeskPlayer[i], false);
        }
		if (msg_playback.Settle != null && msg_playback.Settle.Count > 0)
		{
			foreach (var item in msg_playback.Settle)
			{
				varSeats.GetPlayerByPlayerID(item.PlayerId).PlayCardAniEverySecondNoAni(item);
			}
			msg_playback.Settle.Clear();
		}
	}

	protected override void OnButtonClick(object sender, string btId)
	{
		switch (btId)
		{
			case "关闭":
				GF.UI.Close(this.UIForm);
				break;
		}
	}
}
