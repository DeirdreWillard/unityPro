
using DG.Tweening;
using UnityEngine;
using static UtilityBuiltin;
using UnityEngine.UI;
using NetMsg;
public class BarrageItem : MonoBehaviour
{
    public Text nameText;

    public Text stateTxt;

    public GameObject emjCon;

    public void InitText(Msg_DeskChat t, float y)
    {
        nameText.text = t.Sender.Nick + ": ";
        stateTxt.text = t.Chat;
        transform.transform.localPosition = new Vector3(1000, y, 0);
        transform.DOLocalMoveX(-1300, 20f).OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    public void InitEmoij(Msg_DeskChat t, float y)
    {
        nameText.text = t.Sender.Nick + ": ";
        string s = AssetsPath.GetPrefab("UI/Chat/" + t.Chat);
        GF.UI.LoadPrefab(s, (gameObject) =>
        {
            GameObject a = Instantiate((gameObject as GameObject), emjCon.transform);
        });
        transform.transform.localPosition = new Vector3(1000, y, 0);
        transform.DOLocalMoveX(-1300, 20f).OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}
