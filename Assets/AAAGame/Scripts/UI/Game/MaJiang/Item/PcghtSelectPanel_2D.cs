using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UtilityBuiltin;

/// <summary>
/// 2D麻将碰吃杠听胡选择面板
/// 用于展示多种吃牌组合或多张杠牌的选择界面
/// </summary>
public class PcghtSelectPanel_2D : MonoBehaviour
{
    #region UI节点定义（需要在Unity中创建并绑定）
    
    [Header("===== 面板标题 =====")]
    public Text titleText;                          // 标题文本（"选择吃牌" 或 "选择杠牌"）
    
    [Header("===== 选项容器 =====")]
    public RectTransform optionsContainer;          // 选项容器（用于动态创建选项按钮）
    
    [Header("===== 选项模板 =====")]
    public GameObject optionTemplate;               // 选项模板预制体（包含3张牌的显示）
    
    #endregion
    
    #region 私有变量
    
    /// <summary>当前选择类型</summary>
    private PengChiGangTingHuType currentType;
    
    /// <summary>吃牌组合列表</summary>
    private List<MahjongFaceValue[]> chiCombinations;
    
    /// <summary>杠牌列表</summary>
    private List<MahjongFaceValue> gangCards;
    
    /// <summary>当前选中的索引</summary>
    private int selectedIndex = -1;
    
    /// <summary>选项按钮列表</summary>
    private List<Button> optionButtons = new List<Button>();
    
    /// <summary>选择完成回调</summary>
    private System.Action<int> onConfirmCallback;
    
    #endregion
    
    #region 初始化
    
    void Awake()
    {
        // 初始时隐藏
        gameObject.SetActive(false);
    }
    
    #endregion
    
    #region 显示选择面板
    
    /// <summary>
    /// 显示吃牌选择面板
    /// </summary>
    /// <param name="combinations">吃牌组合列表</param>
    /// <param name="onConfirm">确认回调</param>
    /// <param name="onCancel">取消回调</param>
    public void ShowChiSelection(List<MahjongFaceValue[]> combinations, System.Action<int> onConfirm)
    {
        if (combinations == null || combinations.Count == 0)
        {
            GF.LogWarning("[PcghtSelectPanel_2D] 吃牌组合列表为空");
            return;
        }
        
        currentType = PengChiGangTingHuType.CHI;
        chiCombinations = combinations;
        gangCards = null;
        onConfirmCallback = onConfirm;
        
        // 设置标题
        if (titleText != null)
        {
            titleText.text = "选择吃牌";
        }
        
        // 创建选项
        CreateChiOptions();
        
        // 显示面板
        gameObject.SetActive(true);
        
        GF.LogInfo($"[PcghtSelectPanel_2D] 显示吃牌选择面板，共{combinations.Count}种组合");
    }
    
    /// <summary>
    /// 显示杠牌选择面板
    /// </summary>
    /// <param name="cards">杠牌列表</param>
    /// <param name="onConfirm">确认回调</param>
    /// <param name="onCancel">取消回调</param>
    public void ShowGangSelection(List<MahjongFaceValue> cards, System.Action<int> onConfirm)
    {
        if (cards == null || cards.Count == 0)
        {
            GF.LogWarning("[PcghtSelectPanel_2D] 杠牌列表为空");
            return;
        }
        
        currentType = PengChiGangTingHuType.GANG;
        chiCombinations = null;
        gangCards = cards;
        onConfirmCallback = onConfirm;
        
        // 设置标题
        if (titleText != null)
        {
            titleText.text = "选择杠牌";
        }
        
        // 创建选项
        CreateGangOptions();
        
        // 显示面板
        gameObject.SetActive(true);
        
        GF.LogInfo($"[PcghtSelectPanel_2D] 显示杠牌选择面板，共{cards.Count}种选择");
    }
    
    #endregion
    
    #region 创建选项
    
    /// <summary>
    /// 创建吃牌选项
    /// </summary>
    private void CreateChiOptions()
    {
        ClearOptions();
        
        if (optionsContainer == null || optionTemplate == null)
        {
            GF.LogError("[PcghtSelectPanel_2D] optionsContainer 或 optionTemplate 未设置");
            return;
        }
        
        // 隐藏模板
        optionTemplate.SetActive(false);
        
        for (int i = 0; i < chiCombinations.Count; i++)
        {
            int index = i; // 闭包变量
            MahjongFaceValue[] combo = chiCombinations[i];
            
            // 实例化选项
            GameObject optionObj = Instantiate(optionTemplate, optionsContainer);
            optionObj.SetActive(true);
            
            // 设置牌值显示
            SetOptionCards(optionObj, combo);
            
            // 获取Button组件
            Button button = optionObj.GetComponent<Button>();
            if (button == null)
            {
                button = optionObj.AddComponent<Button>();
            }
            
            // 添加点击事件 - 直接选择并确认
            button.onClick.AddListener(() => {
                OnOptionSelectedAndConfirm(index);
            });
            
            optionButtons.Add(button);
        }
    }
    
    /// <summary>
    /// 创建杠牌选项
    /// </summary>
    private void CreateGangOptions()
    {
        ClearOptions();
        
        if (optionsContainer == null || optionTemplate == null)
        {
            GF.LogError("[PcghtSelectPanel_2D] optionsContainer 或 optionTemplate 未设置");
            return;
        }
        
        // 隐藏模板
        optionTemplate.SetActive(false);
        
        for (int i = 0; i < gangCards.Count; i++)
        {
            int index = i; // 闭包变量
            MahjongFaceValue card = gangCards[i];
            
            // 实例化选项
            GameObject optionObj = Instantiate(optionTemplate, optionsContainer);
            optionObj.SetActive(true);
            
            // 设置牌值显示（杠牌显示4张相同的）
            MahjongFaceValue[] gangCombo = new MahjongFaceValue[] { card, card, card, card };
            SetOptionCards(optionObj, gangCombo);
            
            // 获取Button组件
            Button button = optionObj.GetComponent<Button>();
            if (button == null)
            {
                button = optionObj.AddComponent<Button>();
            }
            
            // 添加点击事件 - 直接选择并确认
            button.onClick.AddListener(() => {
                OnOptionSelectedAndConfirm(index);
            });
            
            optionButtons.Add(button);
        }
    }
    
    /// <summary>
    /// 设置选项中的牌值显示
    /// </summary>
    /// <param name="optionObj">选项对象</param>
    /// <param name="cards">牌值数组</param>
    private void SetOptionCards(GameObject optionObj, MahjongFaceValue[] cards)
    {
        // 在选项对象中查找名为 "Card1", "Card2", "Card3" 的子对象
        // 这些子对象需要有 MjCard_2D 组件
        
        // 获取当前颜色样式
        MjColor currentColor = MahjongSettings.GetCardStyle();
        // 选择面板使用手牌背景（座位0）
        var sprite = MahjongResourceManager.GetBackgroundSprite(currentColor, 0, "hand");

        
        for (int i = 0; i < cards.Length && i < 4; i++)
        {
            string cardName = $"Card{i + 1}";
            Transform cardTransform = optionObj.transform.Find(cardName);
            
            if (cardTransform != null)
            {
                MjCard_2D mjCard = cardTransform.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    mjCard.SetCardValue(cards[i], false);
                    if (sprite != null)
                    {
                        mjCard.SetCardBgSprite(sprite);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 清除所有选项
    /// </summary>
    private void ClearOptions()
    {
        selectedIndex = -1;
        optionButtons.Clear();
        
        if (optionsContainer != null)
        {
            // 清除所有子对象（除了模板）
            for (int i = optionsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = optionsContainer.GetChild(i);
                if (child.gameObject != optionTemplate)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 选项被选中并确认
    /// </summary>
    private void OnOptionSelectedAndConfirm(int index)
    {
        selectedIndex = index;
        
        if (currentType == PengChiGangTingHuType.CHI)
        {
            GF.LogInfo($"[PcghtSelectPanel_2D] 选中并确认吃牌组合: {index}");
        }
        else if (currentType == PengChiGangTingHuType.GANG)
        {
            GF.LogInfo($"[PcghtSelectPanel_2D] 选中并确认杠牌: {index}");
        }
        
        // 调用回调
        onConfirmCallback?.Invoke(selectedIndex);
        
        // 隐藏面板
        Hide();
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        ClearOptions();
        
        currentType = PengChiGangTingHuType.CANCEL;
        chiCombinations = null;
        gangCards = null;
        onConfirmCallback = null;
    }
    
    #endregion
}
