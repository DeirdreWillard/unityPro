
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DZAddCoin : MonoBehaviour
{
	public DZGamePanel GamePanel;
	public List<Toggle> ListAddCoinSelect;
	public List<Toggle> ListAddCoinToggle;
	void Start()
	{
	}

	void OnEnable()
	{
	}


	void OnDisable()
	{
	}

	bool auto = false;
	public void OnClickAddButton(int index) {
		auto = false;
		Refresh();
	}

	public void OnClickSelectButton(int index) {
		for (var i = 0;i < 5;i++) {
			if (ListAddCoinSelect[i].isOn == true && ListAddCoinToggle[index].isOn == true) {
				GamePanel.AddCoinSelect[i] = index;
				PlayerPrefs.SetInt($"DZAddCoinSelect{i + 1}", index);
			}
		}
		Refresh();
	}

	void Refresh() {
		for (var i = 0;i < 5;i++) {
			if (ListAddCoinSelect[i].isOn == true && ListAddCoinToggle[GamePanel.AddCoinSelect[i]].isOn == false && auto == false) {
				auto = true;
				ListAddCoinToggle[GamePanel.AddCoinSelect[i]].isOn = true;
			}
			if (GamePanel.AddCoinSelect[i] == 0) {
				ListAddCoinSelect[i].transform.Find("空").gameObject.SetActive(true);
				ListAddCoinSelect[i].transform.Find("文本").gameObject.SetActive(false);
			}
			else {
				ListAddCoinSelect[i].transform.Find("空").gameObject.SetActive(false);
				ListAddCoinSelect[i].transform.Find("文本").gameObject.SetActive(true);
				ListAddCoinSelect[i].transform.Find("文本").GetComponent<Text>().text = ListAddCoinToggle[GamePanel.AddCoinSelect[i]].transform.Find("文本").GetComponent<Text>().text;
			}
		}
	}
}
