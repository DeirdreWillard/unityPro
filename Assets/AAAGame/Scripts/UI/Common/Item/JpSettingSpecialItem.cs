using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Collections.Generic;
using static GlobalManager;
public class JpSettingSpecialItem : MonoBehaviour
{
    public Text text;
    public InputField input;

    KeyValuePair<int, int> data;
    private int minValue;
    public void SetItem(KeyValuePair<int, int> kvp, MethodType methodType)
    {
        data = kvp;
        text.text = GameUtil.ShowType(kvp.Key, methodType);
        input.text = kvp.Value.ToString();
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener(OnEndEdit);
        DefaultJackpotConfig defaultConfig = GlobalManager.GetInstance().GetJpDefaultData(methodType);
        for (int i = 0; i < defaultConfig.RateConfig.Count; i++)
        {
            if (defaultConfig.RateConfig[i].Key == kvp.Key)
            {
                minValue = kvp.Value;
                break;
            }
        }
    }

    public KeyValuePair<int, int> GetData()
    {
        return new KeyValuePair<int, int>(data.Key, int.Parse(input.text));
    }

    private void OnEndEdit(string value)
    {
        // 确保提交的值不小于
        if (!int.TryParse(value, out int result) || result < minValue || result > 100)
        {
            input.text = minValue.ToString();
        }
    }

}
