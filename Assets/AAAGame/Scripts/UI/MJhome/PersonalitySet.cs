using UnityEngine;
using UnityEngine.UI;
using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class PersonalitySet : UIFormBase
{
    private int currentIndex = 0;//当前选择的背景索引
    private int confirmedIndex = 0; //当前确定的背景索引

    // 背景设置保存的键名
    private const string BACKGROUND_INDEX_KEY = "PersonalitySet_BackgroundIndex";
    
    // 背景确认变化事件
    public static event Action<int> OnBackgroundConfirmed;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitializeBackgroundButtons();
        // 恢复之前保存的背景选择状态
        RestoreSavedBackgroundSelection();
    }
    /// <summary>
    /// 获取本地保存的背景索引
    /// </summary>
    /// <returns>背景索引</returns>
    public int GetSavedBackgroundIndex()
    {
        return PlayerPrefs.GetInt(BACKGROUND_INDEX_KEY, 0); // 默认返回0（第一个背景）
    }
    
    /// <summary>
    /// 初始化背景选择按钮
    /// </summary>
    private void InitializeBackgroundButtons()
    {
        int childCount = varBgContent.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var btn = varBgContent.transform.GetChild(i).GetComponent<Button>();
            if (btn != null)
            {
                // 创建局部变量避免闭包问题
                int buttonIndex = i;
                btn.onClick.RemoveAllListeners(); // 清除旧的监听器
                btn.onClick.AddListener(() => OnBackgroundButtonClick(buttonIndex));
            }
        }
    }

    /// <summary>
    /// 背景按钮点击处理
    /// </summary>
    /// <param name="selectedIndex">选中的背景索引</param>
    private void OnBackgroundButtonClick(int selectedIndex)
    {
        // 取消所有背景的选中状态
        SetAllBackgroundsUnselected();
        // 设置当前选中的背景
        SetBackgroundSelected(selectedIndex, true);
        // 保存当前索引值
        currentIndex = selectedIndex;
        Debug.Log($"选择了背景索引: {selectedIndex}");
    }

    /// <summary>
    /// 设置所有背景为未选中状态
    /// </summary>
    private void SetAllBackgroundsUnselected()
    {
        for (int i = 0; i < varBgContent.transform.childCount; i++)
        {
            SetBackgroundSelected(i, false);
        }
    }

    /// <summary>
    /// 设置指定背景的选中状态
    /// </summary>
    /// <param name="index">背景索引</param>
    /// <param name="isSelected">是否选中</param>
    private void SetBackgroundSelected(int index, bool isSelected)
    {
        var child = varBgContent.transform.GetChild(index);
        var trueObject = child.Find("true");
        trueObject.gameObject.SetActive(isSelected);
    }
    /// <summary>
    /// 恢复之前保存的背景选择状态
    /// </summary>
    private void RestoreSavedBackgroundSelection()
    {
        // 获取之前保存的背景索引
        int savedIndex = GetSavedBackgroundIndex();
        GF.LogInfo_wl("恢复背景设置"+ savedIndex);
        // 设置当前索引和确认索引
        currentIndex = savedIndex;
        confirmedIndex = savedIndex;
        
        // 取消所有背景的选中状态
        SetAllBackgroundsUnselected();
        
        // 设置保存的背景为选中状态
        if (savedIndex >= 0 && savedIndex < varBgContent.transform.childCount)
        {
            SetBackgroundSelected(savedIndex, true);
            Debug.Log($"恢复背景设置: {savedIndex}");
        }
    }

    /// <summary>
    /// 应用选中的背景到游戏中
    /// </summary>
    private void ApplySelectedBackground()
    {
        // 保存背景设置到本地存储
        confirmedIndex = currentIndex;
        PlayerPrefs.SetInt(BACKGROUND_INDEX_KEY, confirmedIndex);
        PlayerPrefs.Save(); // 立即保存到磁盘
        
        Debug.Log($"已应用并保存背景设置，索引: {confirmedIndex}");      
        // 触发背景确认事件
        OnBackgroundConfirmed?.Invoke(confirmedIndex);   
        GF.UI.Close(this.UIForm);
    }
    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "确定设置":
                // 应用当前选择的背景设置
                ApplySelectedBackground();
                break;

        }
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }
}
