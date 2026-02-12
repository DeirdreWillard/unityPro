using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI文本渐变效果组件
/// </summary>
[AddComponentMenu("UI/Effects/UITextGradient")]
public class UITextGradient : BaseMeshEffect
{
    /// <summary>
    /// 顶部颜色
    /// </summary>
    [SerializeField]
    private Color32 topColor = Color.white;

    /// <summary>
    /// 底部颜色
    /// </summary>
    [SerializeField]
    private Color32 bottomColor = Color.black;

    /// <summary>
    /// 顶点列表缓存
    /// </summary>
    private List<UIVertex> mVertexList;
    
    /// <summary>
    /// 修改网格数据，应用渐变效果
    /// </summary>
    /// <param name="vh">顶点辅助类</param>
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
        {
            return;
        }

        if (mVertexList == null)
        {
            mVertexList = new List<UIVertex>();
        }

        vh.GetUIVertexStream(mVertexList);
        ApplyGradient(mVertexList);

        vh.Clear();
        vh.AddUIVertexTriangleStream(mVertexList);
    }

    /// <summary>
    /// 应用渐变效果到顶点列表
    /// </summary>
    /// <param name="vertexList">顶点列表</param>
    private void ApplyGradient(List<UIVertex> vertexList)
    {
        for (int i = 0; i < vertexList.Count;)
        {
            ChangeColor(vertexList, i, topColor);
            ChangeColor(vertexList, i + 1, topColor);
            ChangeColor(vertexList, i + 2, bottomColor);
            ChangeColor(vertexList, i + 3, bottomColor);
            ChangeColor(vertexList, i + 4, bottomColor);
            ChangeColor(vertexList, i + 5, topColor);
            i += 6;
        }
    }

    /// <summary>
    /// 修改顶点颜色
    /// </summary>
    /// <param name="verList">顶点列表</param>
    /// <param name="index">顶点索引</param>
    /// <param name="color">新颜色</param>
    private void ChangeColor(List<UIVertex> verList, int index, Color color)
    {
        UIVertex temp = verList[index];
        temp.color = color;
        verList[index] = temp;
    }
}