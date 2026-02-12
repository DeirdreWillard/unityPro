using GameFramework;
using UnityGameFramework.Runtime;
using static UIFormBase;

public class UIParams : RefParams
{
    public bool? AllowEscapeClose { get; set; } = null;
    public int? SortOrder { get; set; } = null;
    public bool IsSubUIForm { get; set; } = false;
    public UIFormAnimationType AnimationOpen { get; set; } = UIFormAnimationType.None;
    public UIFormAnimationType AnimationClose { get; set; } = UIFormAnimationType.None;
    public GameFrameworkAction<UIFormLogic> OpenCallback { get; set; } = null;
    public GameFrameworkAction<UIFormLogic> CloseCallback { get; set; } = null;
    public GameFrameworkAction<object, string> ButtonClickCallback { get; set; } = null;
    public static UIParams Create(bool? allowEscape = null, int? sortOrder = null, UIFormAnimationType animOpen = UIFormAnimationType.None, UIFormAnimationType animClose = UIFormAnimationType.None)
    {
        var uiParms = ReferencePool.Acquire<UIParams>();
        uiParms.CreateRoot();
        uiParms.AllowEscapeClose = allowEscape;
        uiParms.SortOrder = sortOrder;
        uiParms.IsSubUIForm = false;
        uiParms.AnimationOpen = animOpen;
        uiParms.AnimationClose = animClose;
        return uiParms;
    }


    protected override void ResetProperties()
    {
        base.ResetProperties();
        AllowEscapeClose = null;
        SortOrder = null;
        AnimationOpen = UIFormAnimationType.None;
        AnimationClose = UIFormAnimationType.None;
        OpenCallback = null;
        CloseCallback = null;
        ButtonClickCallback = null;
        IsSubUIForm = false;
    }
}
