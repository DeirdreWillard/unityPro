using DG.Tweening;
using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class BringTips : UIFormBase
{
    [SerializeField] GameObject bringTipsBox = null;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ShowBringTipsBox();
        bringTipsBox.GetComponent<Button>().onClick.AddListener(delegate ()
        {
            BringManager.GetInstance().ShowBringInfoList();
        });

        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        bringTipsBox.GetComponent<Button>().onClick.RemoveAllListeners();
        bringTipsBox.GetComponent<Button>().onClick.RemoveAllListeners();

        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        base.OnClose(isShutdown, userData);
    }

    public void HideBringTipsBox()
    {
        bringTipsBox.transform.DOKill();
        bringTipsBox.SetActive(false);
        GF.UI.Close(this.UIForm);
    }
    public void ShowBringTipsBox()
    {
        bringTipsBox.transform.localScale = new Vector3(1, 1, 1);
        bringTipsBox.transform.DOScale(0.8f, 1).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
        bringTipsBox.SetActive(true);
    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.Msg:
                var infoList = BringManager.GetInstance().GetBringInfoList();
                if (infoList != null && infoList.Count > 0)
                {
                    ShowBringTipsBox();
                }else{
                    HideBringTipsBox();
                }
                break;
        }
    }
}
