using UnityEngine;
using UnityEngine.UI;
using NetMsg;
public class JpSettingItem : MonoBehaviour
{
    public Text baseCoinText;
    public InputField potInput;
    public InputField profitInput;
    public InputField rateInput;

    private Msg_BaseJackpot data;

    public void SetItem(Msg_BaseJackpot msg)
    {
        data = msg;
        baseCoinText.text = float.Parse(data.BaseCoin) + "/" + (float.Parse(data.BaseCoin) * 2);
        potInput.text = "0";
        profitInput.text = float.Parse(data.Profit).ToString();
        rateInput.text = float.Parse(data.Rate).ToString();

        potInput.onEndEdit.RemoveAllListeners();
        potInput.onEndEdit.AddListener(OnEndEdit_pot);
        profitInput.onEndEdit.RemoveAllListeners();
        profitInput.onEndEdit.AddListener(OnEndEdit_profit);
        rateInput.onEndEdit.RemoveAllListeners();
        rateInput.onEndEdit.AddListener(OnEndEdit_rate);
    }

    public Msg_BaseJackpot GetResult()
    {
        Msg_BaseJackpot result = new()
        {
            BaseCoin = data.BaseCoin,
            State = data.State,
            Pot = potInput.text,
            Profit = profitInput.text,
            Rate = rateInput.text,
        };
        return result;
    }
    private void OnEndEdit_pot(string value)
    {
        // 确保提交的值不小于
        if (!float.TryParse(value, out float result) || result < 0)
        {
            potInput.text = "0";
        }
    }
    private void OnEndEdit_profit(string value)
    {
        // float.TryParse(data.BaseCoin, out float baseCoin);
        // 确保提交的值不小于
        if (!float.TryParse(value, out float result) || result < 0)
        {
            profitInput.text = "0";
            // profitInput.text = (baseCoin * 10).ToString();
        }
    }
    
    private void OnEndEdit_rate(string value)
    {
        // float.TryParse(data.BaseCoin, out float baseCoin);
        // 确保提交的值不小于
        if (!float.TryParse(value, out float result) || result < 0)
        {
            rateInput.text = "0";
            // rateInput.text = (baseCoin * 0.5).ToString();
        }
    }
}
