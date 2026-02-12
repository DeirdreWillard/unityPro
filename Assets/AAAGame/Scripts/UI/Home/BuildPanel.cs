using NetMsg;
using UnityEngine;
using UnityEngine.UI;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class BuildPanel : MonoBehaviour
{
    public InputField InputField;
    public async void OnButtonClick(string btId)
    {
        switch (btId)
        {
            case "AddRoom":
                if (int.TryParse(InputField.text, out int deskId))
                {
                    Util.GetInstance().Send_EnterDeskRq(deskId);
                }
                break;
            case "BuildRoom":
                await GF.UI.OpenUIFormAwait(UIViews.CreateRoom);
                break;
        }
    }
}
