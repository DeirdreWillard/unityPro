using UnityEngine;
using UnityEngine.UI;

public class Ping : MonoBehaviour
{
	public Text text;
	public int ping_1 = 100;
	public int ping_2 = 300;

	public Color color_1 = Color.green;
	public Color color_2 = Color.yellow;
	public Color color_3 = Color.red;

	void OnEnable() {
		showPing();
		HotfixNetworkComponent.AddListener(MessageID.MsgPingAck, OnPingRspMsg);
	}

	void OnDisable() {
		HotfixNetworkComponent.RemoveListener(MessageID.MsgPingAck, OnPingRspMsg);
	}

	private void OnPingRspMsg(MessageRecvData data) {
		showPing();
	}

	private void showPing() {
		text.text = $"{HotfixNetworkManager.Ins.GetPing()}ms";
		if (HotfixNetworkManager.Ins.GetPing() < ping_1) {
			text.color = color_1;
		}
		else if (HotfixNetworkManager.Ins.GetPing() < ping_2) {
			text.color = color_2;
		}
		else {
			text.color = color_3;
		}
	}
}
