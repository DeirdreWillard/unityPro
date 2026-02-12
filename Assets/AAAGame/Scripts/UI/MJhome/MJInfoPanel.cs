using UnityEngine;
using UnityEngine.UI;
using NetMsg;

public class MJInfoPanel : MonoBehaviour
{
	public GameObject GoVip;
	public RawImage ImageAvatar;
	public Text TextNick;
	public int MaxChar = -1;
	public void Init(BasePlayer baseplayer)
	{
		Init(baseplayer.Nick, baseplayer.HeadImage, baseplayer.Vip);
	}

	public void Init(UserDataModel userinfo)
	{
		Init(userinfo.NickName, userinfo.HeadIndex, userinfo.VipState);
	}

	public void Init(string nick, string avatar, int vip)
	{
		if (TextNick != null)
		{
			TextNick.FormatNickname(nick, MaxChar);
		}
		if (ImageAvatar != null)
		{
			Util.DownloadHeadImage(ImageAvatar, avatar);
		}
		if (GoVip != null)
		{
			GoVip.SetActive(vip > 0);
		}
	}
	

}
