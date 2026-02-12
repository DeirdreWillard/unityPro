using UnityEngine;
using UnityEngine.UI;
using Google.Protobuf;
using NetMsg;
using System;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class CreateAlliances : UIFormBase
{
    public InputField InputUnionName;
    public InputField InputUnionCoinName;

    public Text TextCostDiamond;
    public Text TextDiamond;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_CreateLeagueRs, FunkMsgCreateLeagueRs);
        InputUnionName.transform.SetLocalPositionY(0);
        OnFunckSystemConfigRs();
    }

    private void OnFunckSystemConfigRs()
    {
        TextCostDiamond.text = GlobalManager.GetConstants(9).ToString();
        TextDiamond.text = Util.GetMyselfInfo().Diamonds.ToString();
    }

    /// <summary>
    ///  创建联盟返回
    /// </summary>
    /// <param name="data"></param>
    private void FunkMsgCreateLeagueRs(MessageRecvData data)
    {
        GF.LogInfo("创建联盟返回");
        Msg_CreateLeagueRs ack = Msg_CreateLeagueRs.Parser.ParseFrom(data.Data);
        GF.UI.Close(this.UIForm);
        Util.UpdateClubInfoRq();
    }

    public RawImage head;
    public void FunckCreateClub()
    {
        if (string.IsNullOrEmpty(InputUnionName.text) || string.IsNullOrEmpty(InputUnionCoinName.text))
        {
            GF.UI.ShowToast("请输入完整信息!");
            return;
        }
        if (!Util.IsValidName(InputUnionName.text))
        {
            InputUnionName.text = "";
            GF.UI.ShowToast("联盟名字含有特殊字符,请重新输入!");
            return;
        }
        if (!Util.IsValidName(InputUnionCoinName.text))
        {
            InputUnionCoinName.text = "";
            GF.UI.ShowToast("联盟币名称含有特殊字符,请重新输入!");
            return;
        }
        try
        {
            Msg_CreateLeagueRq req = MessagePool.Instance.Fetch<Msg_CreateLeagueRq>();
            req.LeagueName = InputUnionName.text;
            req.GoldName = InputUnionCoinName.text;
            req.Type = LeagueType.Union;
            if (head.texture.name != "UnionHead120")
            {
                Texture2D temp = head.texture as Texture2D;
                if (temp == null)
                {
                    GF.LogWarning("头像为空.");
                    return;
                }
                byte[] bytes = PhotoManager.GetInstance().Texture2DTobytes(temp, false, 80);
                req.LeagueHead = ByteString.CopyFrom(bytes);
            }
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_CreateLeagueRq, req);
        }
        catch (Exception ex) 
        {
            GF.LogError("Error while converting texture to bytes: ", ex.Message);
        }

    }
    public void UpHead()
    {
        PhotoManager.GetInstance().TakePhoto(head);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_CreateLeagueRs, FunkMsgCreateLeagueRs);
        base.OnClose(isShutdown, userData);
    }

}
