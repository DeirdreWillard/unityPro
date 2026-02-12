using UnityEngine;
using UnityEngine.UI;
public class ContentSizeFitterTest : MonoBehaviour
{ 
    public Text parentText;         // 父Text参数变量
    public Text showText;           // 显示的Text参数变量        // Update is called once per frame
    private string content;
    
   
    void Update()
    {
        ContentSync();
       
    }

    /// <summary>    /// 把 ShowText的内容同步到ParentText上    /// </summary>
    private void ContentSync()
    {
        content = showText.text;      
        parentText.text = content;
    }
                                     
}