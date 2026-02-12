using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

[System.Serializable]
class MJCSInfo
{
    public string qq;
    public string wechat;
    public string telegram;
    public string jspp;
    public string image;
}

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class MJCustomerService : UIFormBase
{

    private CSInfo csinfo;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.LogInfo("CustomSupportDialog");
        StartCoroutine(getSupportInfo());
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "Support1":
                // 直接打开网站
                if (!string.IsNullOrEmpty(csinfo.telegram))
                {
                    Application.OpenURL(csinfo.telegram);        // 在浏览器中打开网站
                }
                else
                {
                    GF.UI.ShowToast("网站链接无效!");
                }
                break;

            case "Support2":
                GUIUtility.systemCopyBuffer = csinfo.qq;
                GF.UI.ShowToast("复制成功!");
                break;
            case "Support3":
                GUIUtility.systemCopyBuffer = csinfo.jspp;
                GF.UI.ShowToast("复制成功!");
                break;
        }
    }

    IEnumerator getSupportInfo()
    {
        string downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}:{Const.ServerInfo_BackEndPort}/data/contact.php";
        if (Const.CurrentServerType == Const.ServerType.外网服)
        {
            downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/data/contact.php";
        }
        UnityWebRequest request1 = UnityWebRequest.Get(downloadUrl);
        request1.certificateHandler = new BypassCertificate();
        GF.LogInfo("request Support: ", request1.url);
        yield return request1.SendWebRequest();
        transform.Find("Support2").gameObject.SetActive(false);
        if (request1.result == UnityWebRequest.Result.Success)
        {
            GF.LogInfo("SupportInfo:", request1.downloadHandler.text);
            csinfo = JsonUtility.FromJson<CSInfo>(request1.downloadHandler.text);
            if (csinfo.qq != null && csinfo.qq != "")
            {
                transform.Find("Support2").gameObject.SetActive(true);
                transform.Find("Support2/Text").GetComponent<Text>().text = csinfo.qq;
            }
            //wecaht 代替 telegam
            if (csinfo.telegram != null && csinfo.telegram != "")
            {              
            }
            if (csinfo.jspp != null && csinfo.jspp != "")
            {
                transform.Find("Support3").gameObject.SetActive(true);
                transform.Find("Support3/Text").GetComponent<Text>().text = csinfo.jspp;

            }

            if (csinfo.image != null && csinfo.image != "")
            {
                downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}:{Const.ServerInfo_BackEndPort}/data/";
                if (Const.CurrentServerType == Const.ServerType.外网服)
                {
                    downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/data/";
                }
                UnityWebRequest request2 = UnityWebRequestTexture.GetTexture(downloadUrl + csinfo.image);
                request2.certificateHandler = new BypassCertificate();
                yield return request2.SendWebRequest();
                if (request2.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request2);
              
                    GF.LogInfo("request2: ", request2.result.ToString() + " width" + texture.width + " height" + texture.height);
                }
            }
        }
        else
        {
            GF.UI.ShowToast(request1.error);
        }
    }
}
