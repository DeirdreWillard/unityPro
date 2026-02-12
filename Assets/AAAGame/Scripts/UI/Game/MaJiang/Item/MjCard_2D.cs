using UnityEngine;
using UnityEngine.UI;
using static UtilityBuiltin;
using System.Collections.Generic;

public class MjCard_2D : MonoBehaviour
{
    public Image bgImage;
    public Image valueImage;
    public GameObject mark;
    public GameObject laizi;
    public GameObject ting;
    
    /// <summary>
    /// 当前牌值（用于查找手牌索引）
    /// </summary>
    public int cardValue { get; private set; } = -1;

    public MahjongFaceValue mahjongFaceValue { get; private set; } = MahjongFaceValue.MJ_UNKNOWN;

    /// <summary>
    /// 是否是亮牌区的牌（用于区分碰吃杠组和亮牌区）
    /// </summary>
    public bool isLiangPaiCard { get; set; } = false;

    /// <summary>
    /// 是否是变暗的剩余手牌（亮牌后的剩余手牌）
    /// </summary>
    public bool isDimmedCard { get; set; } = false;

    /// <summary>
    /// 是否是甩牌区的牌（血流麻将甩牌，3张不同牌值的牌作为一组）
    /// </summary>
    public bool isShuaiPaiCard { get; set; } = false;

    /// <summary>
    /// 设置麻将牌值（使用统一资源管理器）
    /// </summary>
    /// <param name="value">麻将牌的点数</param>
    /// <param name="isLaizi">是否为赖子</param>
    public void SetCardValue(int value, bool isLaizi = false)
    {
        if ((value < 1 || value > 34) && (value < 41 || value > 43))
        {
            return;
        }
        // 保存牌值
        cardValue = value;
        mahjongFaceValue = Util.ConvertServerIntToFaceValue(value);

        // 从统一资源管理器获取点数图片
        var sprite = MahjongResourceManager.GetPointSprite(value);
        if (sprite != null && valueImage != null)
        {
            valueImage.sprite = sprite;
            valueImage.SetNativeSize();
            valueImage.gameObject.SetActive(true);
        }

        // 设置赖子标记显示状态
        SetLaiziMark(isLaizi);
        SetTingMark(false);
    }

    /// <summary>
    /// 设置麻将牌值
    /// </summary>
    /// <param name="value">麻将牌面值（MahjongFaceValue 枚举）</param>
    /// <param name="isLaizi">是否为赖子</param>
    public void SetCardValue(MahjongFaceValue value, bool isLaizi = false)
    {
        if (value == MahjongFaceValue.MJ_UNKNOWN)
        {
            return;
        }
        // 将 MahjongFaceValue 枚举值转换为图片索引
        int imageIndex = Util.ConvertFaceValueToServerInt(value);
        // 调用int版本的SetCardValue
        SetCardValue(imageIndex, isLaizi);
    }

    /// <summary>
    /// 直接设置麻将牌背景Sprite（性能优化，避免重复加载）
    /// </summary>
    /// <param name="sprite">背景Sprite</param>
    /// <param name="OnlyShowBack">是否只显示背面</param>
    public void SetCardBgSprite(Sprite sprite, bool OnlyShowBack = false)
    {
        if (bgImage == null || sprite == null) return;
        bgImage.sprite = sprite;
        if (valueImage != null)
        {
            valueImage.gameObject.SetActive(!OnlyShowBack);
        }
    }

    #region 遮罩优先级系统

    /// <summary>
    /// 遮罩类型优先级（数值越大优先级越高）
    /// </summary>
    public enum MarkPriority
    {
        None = 0,           // 无遮罩
        PengGang = 10,      // 碰杠标记（可被清除）
        Ting = 20,          // 听牌标记（卡五星亮牌后）
        XueLiuHu = 30,      // 血流胡牌变暗（最高优先级，不可被清除）
    }

    /// <summary>
    /// 当前遮罩优先级
    /// </summary>
    private MarkPriority currentMarkPriority = MarkPriority.None;

    /// <summary>
    /// 当前遮罩颜色
    /// </summary>
    private string currentMarkColor = "";

    #endregion

    public void SetMark(bool show, string color = "#FFFF00")
    {
        if (mark == null) return;
        
        if (show == false)
        {
            if (laizi != null && laizi.activeSelf)
            {
                return;
            }
            mark.SetActive(false);
            currentMarkPriority = MarkPriority.None;
            currentMarkColor = "";
        }
        else
        {
            // 无论mark当前状态如何，总是更新颜色
            var colorObj = mark.transform.GetChild(0).GetComponent<Image>();
            ColorUtility.TryParseHtmlString(color, out Color parsedColor);
            colorObj.color = parsedColor;
            mark.SetActive(true);
        }
    }

    /// <summary>
    /// 设置遮罩（带优先级控制）
    /// </summary>
    /// <param name="show">是否显示</param>
    /// <param name="priority">遮罩优先级</param>
    /// <param name="color">遮罩颜色</param>
    private void SetMarkWithPriority(bool show, MarkPriority priority, string color)
    {
        if (mark == null) return;

        if (show)
        {
            // 显示遮罩：只有当新优先级 >= 当前优先级时才更新
            if (priority >= currentMarkPriority)
            {
                currentMarkPriority = priority;
                currentMarkColor = color;
                SetMark(true, color);
            }
        }
        else
        {
            // 清除遮罩：只有当清除优先级 >= 当前优先级时才清除
            if (priority >= currentMarkPriority)
            {
                SetMark(false);
            }
        }
    }

    /// <summary>
    /// 设置赖子标记显示状态
    /// </summary>
    /// <param name="show">是否显示赖子标记</param>
    public void SetLaiziMark(bool show)
    {
        if (laizi != null && (show != laizi.activeSelf))
        {
            laizi.SetActive(show);
        }
        SetMark(show, "#ffff0050");
    }

    public void SetPengChiGangMark(bool show)
    {
        SetMarkWithPriority(show, MarkPriority.PengGang, "#00FFFF50");
    }

    /// <summary>
    /// 设置听牌标记（卡五星亮牌后可听的牌）
    /// </summary>
    public void SetTingPaiMark(bool show)
    {
        SetMarkWithPriority(show, MarkPriority.Ting, "#FFFF0050");
    }

    /// <summary>
    /// 设置血流胡牌变暗效果（最高优先级）
    /// </summary>
    public void SetXueLiuHuMark(bool show)
    {
        SetMarkWithPriority(show, MarkPriority.XueLiuHu, "#00000032");
    }

    public void SetTingMark(bool show)
    {
        if (ting != null && (show != ting.activeSelf))
        {
            ting.SetActive(show);
        }
    }
    
}
