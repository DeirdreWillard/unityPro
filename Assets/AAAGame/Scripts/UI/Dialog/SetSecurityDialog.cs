using NetMsg;
using UnityEngine;
using Google.Protobuf;
using static UtilityBuiltin;
using UnityGameFramework.Runtime;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class SetSecurityDialog : UIFormBase
{

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.ModifyUserAck, OnifyUserAck);
        varNicknameInput.text = "";
        varSafeCodeInput.text = "";
        //58张默认头像编号是1-58随机一个下载
        int randomIndex = Random.Range(1, 59);
        GF.LogInfo("修改系统默认头像 :" , randomIndex.ToString());
        string s = AssetsPath.GetSpritesPath($"Avatar/{randomIndex}.png");
        UIExtension.LoadSprite(null, s, (sprite) => {
			varAvatar.sprite = sprite;
		});
    }

    public void OnifyUserAck(MessageRecvData data)
    {   
        Msg_ModifyUserRs ack = Msg_ModifyUserRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到 改名回包 :" , ack.ToString());
        Util.GetMyselfInfo().NickName = ack.Nick;
        Util.GetMyselfInfo().HeadIndex = ack.HeadImage;
        Util.GetMyselfInfo().ModifyNickNum = ack.ModifyNickNum;
        Util.GetMyselfInfo().ModifyHeadNum = ack.ModifyHeadNum;
        Util.GetMyselfInfo().LockState = 0;
        GF.UI.ShowToast("设置成功!");
        GF.UI.Close(this.UIForm);
    }
    
    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "SureBtn":
                Msg_ModifyUserRq req = MessagePool.Instance.Fetch<Msg_ModifyUserRq>();
                if (string.IsNullOrEmpty(varNicknameInput.text))
                {
                    GF.UI.ShowToast("请输入昵称");
                    return;
                }
                if (varNicknameInput.text == Util.GetMyselfInfo().NickName)
                {
                    GF.UI.ShowToast("没有改动!");
                    return;
                }
                if (!Util.IsValidName(varNicknameInput.text))
                {
                    varNicknameInput.text = "";
                    GF.UI.ShowToast("昵称含有特殊字符,请重新输入!");
                    return;
                }
                req.Nick = varNicknameInput.text;
                if (varSafeCodeInput.text.Length != 6)
                {
                    GF.UI.ShowToast("请输入6位钱包密码");
                    return;
                }
                req.SafeCode = varSafeCodeInput.text;
                if (varAvatar.sprite.name != "PlayerHead120")
                {
                    Texture2D temp = varAvatar.sprite.texture as Texture2D;
                    if (temp == null)
                    {
                        GF.LogWarning("头像为空.");
                        return;
                    }
                    byte[] bytes = PhotoManager.GetInstance().Texture2DTobytes(temp, false, 80);
                    req.HeadImage = ByteString.CopyFrom(bytes);
                }
                else
                {
                    req.HeadImage = ByteString.Empty;
                }
                GF.LogInfo("发送 改名请求 :" , req.ToString());
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.ModifyUserReq, req);
                break;
            case "UpdateAvatar":
                var uiParams = UIParams.Create();
                uiParams.Set<VarGameObject>("lastPanel", gameObject);
                await GF.UI.OpenUIFormAwait(UIViews.UpdateHeadPanel,uiParams);
                // PhotoManager.GetInstance().TakePhoto(varAvatar);
                break;
        }
    }

    
}
