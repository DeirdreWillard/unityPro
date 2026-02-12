using UnityEngine.UI;
using NetMsg;
using UnityEngine;
using Google.Protobuf;
using static UtilityBuiltin;
using UnityGameFramework.Runtime;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class UpdateHeadPanel : UIFormBase
{
    public Text TextCostDiamond2;
    public Text TextDiamond;

    bool hasChanged = false;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.ModifyUserAck, OnifyUserAck);
        Util.DownloadHeadImage(varAvatar, Util.GetMyselfInfo().HeadIndex);
        hasChanged = false;
        OnFunckSystemConfigRs();
        InitList();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.ModifyUserAck, OnifyUserAck);
        base.OnClose(isShutdown, userData);
    }

    private void OnFunckSystemConfigRs()
    {
        TextCostDiamond2.text = Util.GetMyselfInfo().ModifyHeadNum > 0 ? GlobalManager.GetConstants(10).ToString() : "首次免费";
        TextDiamond.text = Util.GetMyselfInfo().Diamonds.ToString();
    }

    public void OnifyUserAck(MessageRecvData data)
    {   
        Msg_ModifyUserRs ack = Msg_ModifyUserRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到 修改头像回包 :" , ack.ToString());
        GF.UI.Close(this.UIForm);
    }

    public void UpdateHeadImg(int index)
    {
        hasChanged = true;
        string s = AssetsPath.GetSpritesPath($"Avatar/{index}.png");
        UIExtension.LoadSprite(null, s, (sprite) =>
        {
            if (sprite != null && sprite.texture != null)
            {
                // 如果是从图集中的 Sprite，需要创建新的 Texture2D
                if (sprite.packed)
                {
                    // 从图集中提取 Sprite 区域创建新纹理
                    Texture2D originalTexture = sprite.texture;
                    Rect textureRect = sprite.textureRect;
                    
                    Texture2D newTexture = new Texture2D((int)textureRect.width, (int)textureRect.height, TextureFormat.RGBA32, false);
                    Color[] pixels = originalTexture.GetPixels((int)textureRect.x, (int)textureRect.y, (int)textureRect.width, (int)textureRect.height);
                    newTexture.SetPixels(pixels);
                    newTexture.Apply();
                    
                    varAvatar.texture = newTexture;
                }
                else
                {
                    // 普通纹理直接使用
                    varAvatar.texture = sprite.texture;
                }
            }
        });
    }

    public void InitList()
    {
        //创建列表
        foreach (Transform item in varContent.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 1; i < 59; i++)
        {
            int index = i;
            GameObject newItem = Instantiate(varItem);
            newItem.transform.SetParent(varContent.transform, false);
            string s = AssetsPath.GetSpritesPath($"Avatar/{index}.png");
            UIExtension.LoadSprite(null, s, (sprite) =>
            {
                newItem.transform.Find("UpdateAvatar/Avatar").GetComponent<Image>().sprite = sprite;
            });
            Button button = newItem.transform.Find("UpdateAvatar").GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate ()
            {
                if (Util.IsClickLocked()) return;
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                UpdateHeadImg(index);
            });
        }
        varScroll.normalizedPosition = new Vector2(0, 1);
    }
    
    protected override void OnButtonClick(object sender, string btId)
    {
       base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "ButOkSetHead":
                if (!hasChanged)
                {
                    GF.UI.ShowToast("没有改动!");
                    return;
                }

                //如果是从SetSecurityDialog进入
                Params.TryGet<VarGameObject>("lastPanel", out VarGameObject goTemp);
                if (goTemp != null){
                    GameObject go = goTemp;
                    go.TryGetComponent<SetSecurityDialog>(out SetSecurityDialog setSecurityDialog);
                    setSecurityDialog.varAvatar.sprite = Sprite.Create(varAvatar.texture as Texture2D, new Rect(0, 0, varAvatar.texture.width, varAvatar.texture.height), new Vector2(0.5f, 0.5f));
                    GF.UI.Close(this.UIForm);
                }
                else
                {
                    Util.GetInstance().OpenConfirmationDialog("修改头像", "确定要修改头像吗?", () =>
                                    {
                                        Msg_ModifyUserRq req = MessagePool.Instance.Fetch<Msg_ModifyUserRq>();
                                        if (varAvatar.texture != null)
                                        {
                                            Texture2D temp = varAvatar.texture as Texture2D;
                                            if (temp == null)
                                            {
                                                GF.LogWarning("头像为空.");
                                                return;
                                            }
                                            byte[] bytes = PhotoManager.GetInstance().Texture2DTobytes(temp, false, 80);
                                            req.HeadImage = ByteString.CopyFrom(bytes);
                                        }
                                        GF.LogInfo("发送 头像修改请求");
                                        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.ModifyUserReq, req);
                                    });
                }
                break;
            case "UpdateAvatar":
                hasChanged = true;
                PhotoManager.GetInstance().TakePhoto(varAvatar);
                break;
        }
    }
    
}
