using System.Collections.Generic;
using DG.Tweening;
using NetMsg;
using UnityEngine;
using static GlobalManager;

public class SettingJp : MonoBehaviour
{
    public GameObject jpSettingItem;
    public GameObject jpSettingSpecialItem;
    public GameObject content;
    public GameObject midInfoObj;

    private Msg_JackpotConfig config;

    private Dictionary<string, GameObject> jpSettingItems = new();
    private Dictionary<int, GameObject> jpSettingSpecialItems = new();

    public void OnClose()
    {
        transform.DOLocalMoveX(-1200, 0.3f).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    public void InitPanel(Msg_JackpotConfig msg_JackpotConfig)
    {
        this.config = msg_JackpotConfig;
        InitList();
        midInfoObj.transform.SetAsLastSibling();
        InitSpecialList();
    }


    public void InitList()
    {
        //创建列表
        foreach (Transform item in content.transform)
        {
            if (item.name != "MidInfoObj")
            {
                Destroy(item.gameObject);
            }
        }
        for (int i = 0; i < config.BaseConfig.Count; i++)
        {
            GameObject newItem = Instantiate(jpSettingItem);
            newItem.transform.SetParent(content.transform, false);
            newItem.GetComponent<JpSettingItem>().SetItem(config.BaseConfig[i]);
            jpSettingItems[config.BaseConfig[i].BaseCoin] = newItem;
        }
    }
    public void InitSpecialList()
    {
        foreach (var item in config.RateConfig)
        {
            GameObject newItem = Instantiate(jpSettingSpecialItem);
            newItem.transform.SetParent(content.transform, false);
            newItem.GetComponent<JpSettingSpecialItem>().SetItem(new KeyValuePair<int, int>(item.Key, item.Val), config.MethodType);
            jpSettingSpecialItems[item.Key] = newItem;
        }
    }

    public Msg_JackpotConfig GetUpdateData()
    {
        for (int i = 0; i < config.BaseConfig.Count; i++)
        {
            config.BaseConfig[i] = jpSettingItems[config.BaseConfig[i].BaseCoin].GetComponent<JpSettingItem>().GetResult();
        }
        for (int i = 0; i < config.RateConfig.Count; i++)
        {
            KeyValuePair<int, int> data = jpSettingSpecialItems[config.RateConfig[i].Key].GetComponent<JpSettingSpecialItem>().GetData();
            config.RateConfig[i].Val = data.Value;
        }
        return config;
    }

    public Msg_JackpotConfig SetDefault()
    {
        MethodType methodType = transform.parent.GetComponent<ClubJackpotPanel>().chooseMethodType;
        DefaultJackpotConfig defaultConfig = GlobalManager.GetInstance().GetJpDefaultData(methodType);
        // 清空并重新赋值现有配置
        config.BaseConfig.Clear();
        foreach (var baseJackpot in defaultConfig.BaseConfig)
        {
            config.BaseConfig.Add(baseJackpot);
        }

        config.RateConfig.Clear();
        foreach (var rateConfig in defaultConfig.RateConfig)
        {
            config.RateConfig.Add(rateConfig);
        }

        config.Donate = defaultConfig.Donate;

        // 调用 InitPanel 方法刷新界面
        InitPanel(config);
        return config;
    }

}
