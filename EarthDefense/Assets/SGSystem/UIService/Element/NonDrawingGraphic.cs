using UnityEngine;
using UnityEngine.UI;

public class NonDrawingGraphic : Graphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); // DESC :: 아무것도 그리지 않음
    }
}