
using UnityEngine;
using UnityEngine.UI;
using static UtilityBuiltin;

public class DZCard : MonoBehaviour
{
	public int color;
	public int value;
	public int numD;

	public Image ImageCard;
	public Image ImageDefault;

	string LastCardPath = "";

	void Start()
	{
		color = -1;
		value = -1;
	}

	public void Init(int num)
	{
		numD = num;
		string CardPath;
		if (num >= 0) {
			color = GameUtil.GetInstance().GetColor(num);
			value = GameUtil.GetInstance().GetValue(num);
			CardPath = "Common/Cards/" + color + value+".png";
		}
		else if(num == -2) {
			CardPath = "Common/Cards/-2.png";
		}
		else {
			CardPath = "Common/Cards/-1.png";
		}
		if (CardPath == LastCardPath) {
			return;
		}

		string s = AssetsPath.GetSpritesPath(CardPath);
		ImageCard.gameObject.SetActive(false);
		ImageDefault.gameObject.SetActive(true);
		GF.UI.LoadSprite(s, (sprite) => {
			ImageCard.sprite = sprite;
			ImageCard.gameObject.SetActive(true);
			ImageDefault.gameObject.SetActive(false);
			LastCardPath = CardPath;
		});
	}

	public void SetImgColor(Color color)
	{
        if (ImageCard!= null)
            ImageCard.color = color;
		if (ImageDefault != null)
			ImageDefault.color = color;
    }
}
