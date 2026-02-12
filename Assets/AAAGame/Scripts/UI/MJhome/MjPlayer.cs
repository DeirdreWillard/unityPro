using System.Linq;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
public class MjPlayer : MonoBehaviour
{
    public Text playerNameTxt;
    public RawImage headImg;
    public void Init(BasePlayer player)
    {
        playerNameTxt.FormatNickname(player.Nick);
        Util.DownloadHeadImage(headImg, player.HeadImage);
        gameObject.SetActive(true);
    }
}