using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
 
[AddComponentMenu("UI/Effects/TextSpacing")]
public class TextSpacing : BaseMeshEffect
{
 
    [SerializeField]
    private float spacing_x;
    //[SerializeField]
    private float spacing_y=0;
    private const int VERTEXT_RANGE = 6;
 
    private List<UIVertex> mVertexList;
 
    public override void ModifyMesh(VertexHelper vh)
    {
        if (spacing_x == 0 && spacing_y == 0) { return; }
        if (!IsActive()) { return; }
        int count = vh.currentVertCount;
        if (count == 0) { return; }
        if (mVertexList == null) { mVertexList = new List<UIVertex>(); }
        vh.GetUIVertexStream(mVertexList);
        int row = 1;
        int column = 2;
        List<UIVertex> sub_vertexs = mVertexList.GetRange(0, VERTEXT_RANGE);
        float min_row_left = sub_vertexs.Min(v => v.position.x);
        int vertex_count = mVertexList.Count;
        for (int i = VERTEXT_RANGE; i < vertex_count;)
        {
            if (i % VERTEXT_RANGE == 0)
            {
                sub_vertexs = mVertexList.GetRange(i, VERTEXT_RANGE);
                float tem_row_left = sub_vertexs.Min(v => v.position.x);
                if (min_row_left - tem_row_left >= -10)
                {
                    min_row_left = tem_row_left;
                    ++row;
                    column = 1;
                    //continue;
                }
            }
 
            for (int j = 0; j < VERTEXT_RANGE; j++)
            {
                UIVertex vertex = mVertexList[i];
                vertex.position += Vector3.right * (column - 1) * spacing_x;
                vertex.position += Vector3.down * (row - 1) * spacing_y;
                mVertexList[i] = vertex;
                ++i;
            }
            ++column;
 
        }
        vh.Clear();
        vh.AddUIVertexTriangleStream(mVertexList);
    }
}