using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using DG.Tweening;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.ObjectPool;
using UnityEngine.Events;

/// <summary>
/// UI基类, 所有UI界面需继承此类
/// </summary>
public class UIFormBase : UIFormLogic, ISerializeFieldTool
{
    [HideInInspector][SerializeField] SerializeFieldData[] _fields;
    public enum UIFormAnimationType
    {
        None,       //无动画
        FadeIn,     //透明淡入
        FadeOut,    //透明淡出
        ScaleIn,    //缩放淡入
        ScaleOut,    //缩放淡出
        MoveIn,    //移动进入
        MoveOut,    //移动出去
        MoveInUp,    //移动进入上

    }
    [HideInInspector][SerializeField] protected RectTransform topBar;
    public SerializeFieldData[] SerializeFieldArr { get => _fields; set => _fields = value; }
    public UIParams Params { get; private set; }
    public int SortOrder => UICanvas.sortingOrder;
    public int Id => this.UIForm.SerialId;
    public bool Interactable
    {
        get
        {
            return canvasGroup.interactable;
        }
        set
        {
            canvasGroup.interactable = value;
        }
    }
    private CanvasGroup canvasGroup = null;
    protected Canvas UICanvas { get; private set; }

    private bool isOnEscape;
    IList<IObjectPool<UIItemObject>> m_ItemPools = null;
    /// <summary>
    /// 子UI界面, 会随着父界面关闭而关闭
    /// </summary>
    IList<int> m_SubUIForms = null;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        Array.Clear(_fields, 0, _fields.Length);
        Params = userData as UIParams;
        UICanvas = gameObject.GetOrAddComponent<Canvas>();
        canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        RectTransform transform = GetComponent<RectTransform>();
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = Vector2.zero;
        transform.localPosition = Vector3.zero;
        gameObject.GetOrAddComponent<GraphicRaycaster>();
        InitLocalization();
        // 刘海屏适配已移至UI Group层级的SafeAreaAdapter组件自动处理
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Params = userData as UIParams;
        var cvs = GetComponent<Canvas>();
        cvs.overrideSorting = true;
        cvs.sortingOrder = Params.SortOrder.Value;
        Interactable = false;
        isOnEscape = Params.AllowEscapeClose.Value;
        PlayUIAnimation(Params.AnimationOpen, OnOpenAnimationComplete);
        Params.OpenCallback?.Invoke(this);
        if (transform.Find("Title/Close"))
        {
            topBar = transform.Find("Title").GetComponent<RectTransform>();
            transform.Find("Title/Close").GetComponent<Button>().onClick.RemoveAllListeners();
            transform.Find("Title/Close").GetComponent<Button>().onClick.AddListener(OnClickClose);
        }
    }


    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (isOnEscape && Input.GetKeyDown(KeyCode.Escape) && GF.UI.GetTopUIFormId() == this.UIForm.SerialId)
        {
            // GF.LogError("OnEscape" + this.UIForm.SerialId);
            this.OnClickClose();
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        DOTween.Kill(this);
        if (!isShutdown)
        {
            Params.CloseCallback?.Invoke(this);
            ReferencePool.Release(Params);
            CloseAllSubUIForms();
        }
        UnspawnAllItemObjects();
        if (transform.Find("Title/Close"))
        {
            transform.Find("Title/Close").GetComponent<Button>().onClick.RemoveListener(OnClickClose);
        }
        base.OnClose(isShutdown, userData);
    }

    private void OnDestroy()
    {
        DestroyAllItemPool();
    }

    private void PlayUIAnimation(UIFormAnimationType animType, GameFrameworkAction onAnimComplete)
    {
        if (null == canvasGroup)
        {
            onAnimComplete.Invoke();
            return;
        }
        switch (animType)
        {
            case UIFormAnimationType.None:
                onAnimComplete.Invoke();
                break;
            case UIFormAnimationType.FadeIn:
                DoFadeAnim(0, 1, 0.4f, onAnimComplete);
                break;
            case UIFormAnimationType.FadeOut:
                DoFadeAnim(1, 0, 0.2f, onAnimComplete);
                break;
            case UIFormAnimationType.MoveIn:
                transform.SetLocalPositionX(1200);
                transform.DOLocalMoveX(0, 0.3f)
                .SetTarget(this)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (GF.UI.IsValidUIForm(this.UIForm))
                    {
                        onAnimComplete?.Invoke();
                    }
                });
                break;
            case UIFormAnimationType.MoveOut:
                transform.DOLocalMoveX(1200, 0.3f)
                .SetTarget(this)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (GF.UI.IsValidUIForm(this.UIForm))
                    {
                        onAnimComplete?.Invoke();
                    }
                });
                break;
            case UIFormAnimationType.ScaleIn:
                // 设置初始状态
                transform.localScale = Vector3.zero;
                canvasGroup.alpha = 0f;
                // 同时执行缩放和透明度动画
                DOTween.Sequence()
                    .Join(transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack))
                    .Join(canvasGroup.DOFade(1f, 0.3f))
                    .SetTarget(this)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (GF.UI.IsValidUIForm(this.UIForm))
                        {
                            onAnimComplete?.Invoke();
                        }
                    });
                break;
            case UIFormAnimationType.ScaleOut:
                DOTween.Sequence()
                    .Join(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack))
                    .Join(canvasGroup.DOFade(0f, 0.3f))
                    .SetTarget(this)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (GF.UI.IsValidUIForm(this.UIForm))
                        {
                            onAnimComplete?.Invoke();
                        }
                    });
                break;
            case UIFormAnimationType.MoveInUp:
                transform.SetLocalPositionY(-1200);
                canvasGroup.alpha = 0f;
                DOTween.Sequence()
                    .SetUpdate(true)
                    .Join(transform.DOLocalMoveY(0, 0.3f))
                    .Join(canvasGroup.DOFade(1f, 0.3f))
                    .SetTarget(this)
                    .OnComplete(() =>
                    {
                        if (GF.UI.IsValidUIForm(this.UIForm))
                        {
                            onAnimComplete?.Invoke();
                        }
                    });
                break;
        }
    }
    /// <summary>
    /// 打开子UI Form
    /// </summary>
    /// <param name="viewName">界面枚举</param>
    /// <param name="subUiOrder">子界面的显示层级(相对父界面)</param>
    /// <param name="params">UI参数</param>
    /// <returns></returns>
    public int OpenSubUIForm(UIViews viewName, int subUiOrder = -1, UIParams @params = null)
    {
        if (m_SubUIForms == null) m_SubUIForms = new List<int>(2);
        @params ??= UIParams.Create();
        if (subUiOrder <= -1) subUiOrder = m_SubUIForms.Count;
        @params.SortOrder = Params.SortOrder + subUiOrder + 1;
        @params.IsSubUIForm = true;
        var uiformId = GF.UI.OpenUIForm(viewName, @params);
        m_SubUIForms.Add(uiformId);
        return uiformId;
    }
    /// <summary>
    /// 关闭子UI Form
    /// </summary>
    /// <param name="uiformId"></param>
    public void CloseSubUIForm(int uiformId)
    {
        if (!m_SubUIForms.Contains(uiformId)) return;
        m_SubUIForms.Remove(uiformId);
        if (GF.UI.HasUIForm(uiformId))
            GF.UI.CloseUIForm(uiformId);
    }
    /// <summary>
    /// 关闭全部子UI Form
    /// </summary>
    public void CloseAllSubUIForms()
    {
        if (m_SubUIForms != null)
        {
            for (int i = m_SubUIForms.Count - 1; i >= 0; i--)
            {
                CloseSubUIForm(m_SubUIForms[i]);
            }
        }
    }

    private void UnspawnAllItemObjects()
    {
        if (m_ItemPools == null) return;
        foreach (var item in m_ItemPools)
        {
            item.ReleaseAllUnused();
            item.UnspawnAll();
        }
    }
    private void DestroyAllItemPool()
    {
        if (m_ItemPools == null) return;

        for (int i = 0; i < m_ItemPools.Count; i++)
        {
            var item = m_ItemPools[i];
            GF.ObjectPool.DestroyObjectPool(item);
        }
        m_ItemPools.Clear();
    }

    /// <summary>
    /// 从对象池获取一个Item (界面关闭时会自动Unspawn)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="itemTemple">Item实例化模板</param>
    /// <param name="instanceRoot">Item实例化到根节点</param>
    /// <param name="capacity">对象池容量</param>
    /// <param name="expireTime">对象过期时间(过期后自动销毁)</param>
    /// <returns></returns>
    protected T SpawnItem<T>(GameObject itemTemple, Transform instanceRoot, float autoReleaseInterval = 5f, int capacity = 50, float expireTime = 50) where T : UIItemObject, new()
    {
        var itemTempleId = itemTemple.GetInstanceID().ToString();
        GameFramework.ObjectPool.IObjectPool<T> pool;
        if (GF.ObjectPool.HasObjectPool<T>(itemTempleId))
        {
            pool = GF.ObjectPool.GetObjectPool<T>(itemTempleId);
        }
        else
        {
            pool = GF.ObjectPool.CreateSingleSpawnObjectPool<T>(itemTempleId, autoReleaseInterval, capacity, expireTime, 0);
            if (m_ItemPools == null) m_ItemPools = new List<IObjectPool<UIItemObject>>();
            m_ItemPools.Add((IObjectPool<UIItemObject>)(object)pool);
        }

        var spawn = pool.Spawn();
        if (spawn == null)
        {
            var itemInstance = Instantiate(itemTemple, instanceRoot);
            spawn = UIItemObject.Create<T>(itemInstance);
            pool.Register(spawn, true);
        }
        return spawn;
    }
    /// <summary>
    /// 从对象池回收Item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="itemTemple">Item实例化模板</param>
    /// <param name="itemObject">要回收的Item实例</param>
    protected void UnspawnItem<T>(GameObject itemTemple, T itemObject) where T : UIItemObject, new()
    {
        UnspawnItem<T>(itemTemple, itemObject.gameObject);
    }
    /// <summary>
    /// 从对象池回收Item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="itemTemple">Item实例化模板</param>
    /// <param name="itemInstance">要回收的Item实例</param>
    protected void UnspawnItem<T>(GameObject itemTemple, GameObject itemInstance) where T : UIItemObject, new()
    {
        var itemTempleId = itemTemple.GetInstanceID().ToString();
        if (!GF.ObjectPool.HasObjectPool<T>(itemTempleId)) return;

        var pool = GF.ObjectPool.GetObjectPool<T>(itemTempleId);
        pool.Unspawn(itemInstance);
    }
    /// <summary>
    /// 回收所有item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="itemTemple"></param>
    protected void UnspawnAllItem<T>(GameObject itemTemple) where T : UIItemObject, new()
    {
        var itemTempleId = itemTemple.GetInstanceID().ToString();
        if (!GF.ObjectPool.HasObjectPool<T>(itemTempleId)) return;

        var pool = GF.ObjectPool.GetObjectPool<T>(itemTempleId);
        pool.ReleaseAllUnused();
        pool.UnspawnAll();
    }
    /// <summary>
    /// 更新界面中静态文本的多语言文字
    /// </summary>
    public virtual void InitLocalization()
    {
        UIStringKey[] texts = GetComponentsInChildren<UIStringKey>(true);
        foreach (var t in texts)
        {
            if (t.TryGetComponent<TMPro.TextMeshProUGUI>(out var textMeshCom))
            {
                textMeshCom.text = GF.Localization.GetString(t.Key);
            }
            else if (t.TryGetComponent<Text>(out var textCom))
            {
                textCom.text = GF.Localization.GetString(t.Key);
            }
        }
    }
    [Obfuz.ObfuzIgnore]
    public void CloseWithAnimation()
    {
        Interactable = false;
        PlayUIAnimation(Params.AnimationClose, OnCloseAnimationComplete);
    }
    public virtual void OnClickClose()
    {
        if (Util.IsClickLocked()) return;
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        GF.UI.Close(this.UIForm);
    }
    [Obfuz.ObfuzIgnore]
    public void ClickUIButton(string bt_tag)
    {
        if (Util.IsClickLocked()) return;
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        OnButtonClick(this, bt_tag);
    }
    [Obfuz.ObfuzIgnore]
    public void ClickUIButton(Button btSelf)
    {
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        OnButtonClick(this, btSelf);
    }
    protected virtual void OnButtonClick(object sender, string btId)
    {
        Params.ButtonClickCallback?.Invoke(sender, btId);
    }
    protected virtual void OnButtonClick(object sender, UnityEngine.UI.Button btSelf)
    {
    }
    /// <summary>
    /// UI打开动画完成时回调
    /// </summary>
    protected virtual void OnOpenAnimationComplete()
    {
        Interactable = true;
    }
    /// <summary>
    /// UI关闭动画完成时回调
    /// </summary>
    protected virtual void OnCloseAnimationComplete()
    {
        // 检查UIForm是否仍然有效（可能已被ReSetUI强制关闭）
        if (GF.UI.IsValidUIForm(this.UIForm))
        {
            GF.UI.CloseUIForm(this.UIForm);
        }
    }

    #region 默认UI动画
    private void DoFadeAnim(float s, float e, float time, GameFrameworkAction onComplete = null)
    {
        canvasGroup.alpha = s;
        var fade = canvasGroup.DOFade(e, time);
        fade.SetEase(Ease.InOutFlash);
        fade.SetTarget(this);
        fade.SetUpdate(true);
        fade.onComplete = () =>
        {
            if (GF.UI.IsValidUIForm(this.UIForm))
            {
                onComplete?.Invoke();
            }
        };
    }
    #endregion
}

[Serializable]
public class SerializeFieldData
{
    public string VarName;      //变量名
    public GameObject[] Targets;//关联的GameObject
    public string VarType;      //变量类型FullName,带有名字空间
    public int VarPrefix;//变量private/protect/public
    public SerializeFieldData(string varName, GameObject[] targets = null)
    {
        VarName = varName;
        Targets = targets;
    }
}
public interface ISerializeFieldTool
{
    public SerializeFieldData[] SerializeFieldArr { get; set; }
}
