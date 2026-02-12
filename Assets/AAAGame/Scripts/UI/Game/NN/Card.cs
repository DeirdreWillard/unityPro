using UnityEngine;
using UnityEngine.UI;
using static UtilityBuiltin;
using DG.Tweening;

public class Card : MonoBehaviour
{
    public int color;
    public int value;
    public Image image;
    public int numD;//二进制值判断牌型
    // Start is called before the first frame update
    void Start()
    {
        color = -1;
        value = -1;
    }

    public void Init(int num)
    {
        numD = num;
        string CardPath;
        if (num >= 0)
        {    
            //花色固定4  78 79
            color = GameUtil.GetInstance().GetColor(num);
            value = GameUtil.GetInstance().GetValue(num);
            CardPath = "Common/Cards/" + color + value+".png";
        }
        else if(num == -2) {
            CardPath = "Common/Cards/-2.png";
        }else{
            CardPath = "Common/Cards/-1.png";
        }
        string s = AssetsPath.GetSpritesPath(CardPath);
        GF.UI.LoadSprite(s, (sprite) =>{
            if (gameObject == null) return;
            transform.TryGetComponent<Image>(out image);
            if (image == null)
            {
                gameObject.AddComponent<Image>();
                image = gameObject.GetComponent<Image>();
            }
            image.sprite = sprite;
        });
    }

    public void InitByName(string name)
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(true);
        transform.TryGetComponent<Image>(out image);
        if (image == null)
        {
            gameObject.AddComponent<Image>();
            image = gameObject.GetComponent<Image>();
        }
        // image.sprite = CoroutineRunner.Instance.GetCardSprite(name);
            
        string CardPath = "Common/Cards/" + name + ".png";
        string s = AssetsPath.GetSpritesPath(CardPath);
        GF.UI.LoadSprite(s, (sprite) =>{
            transform.TryGetComponent<Image>(out image);
            if (image == null)
            {
                gameObject.AddComponent<Image>();
                image = gameObject.GetComponent<Image>();
            }
            image.sprite = sprite;
        });
    }

    public void SetImgColor(Color color)
    {
        if (image!= null)
            image.color = color;
    }

    /// <summary>
    /// 通用翻牌动画
    /// </summary>
    /// <param name="_cardValue">要显示的牌值</param>
    /// <param name="_duration">翻转动画时长</param>
    /// <param name="_onComplete">动画完成回调</param>
    public void InitAni(int _cardValue, float _duration = 0.2f, System.Action _onComplete = null)
    {
        // 第一阶段：翻转到90度
        transform.DORotate(new Vector3(0, 90, 0), _duration).OnComplete(() =>
        {
            // 在90度时更新牌面
            Init(_cardValue);
            
            // 第二阶段：翻转回0度
            transform.DORotate(Vector3.zero, _duration).OnComplete(() =>
            {
                if (gameObject == null) return;
                
                // 确保旋转角度为标准值
                transform.localRotation = Quaternion.identity;
                
                // 执行完成回调
                _onComplete?.Invoke();
            });
        });
    }
    
}
