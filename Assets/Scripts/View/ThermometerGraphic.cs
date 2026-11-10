using UnityEngine;
using UnityEngine.UI;

/// <summary>Responsive, non-interactive thermometer strokes beneath each cell’s digits.</summary>
public sealed class ThermometerGraphic : MaskableGraphic
{
    private Thermometer[] paths;
    private SudokuCell[,] cells;
    private int cellIndex;
    public void SetPaths(Thermometer[] value, SudokuCell[,] grid, int index)
    {
        paths = value;
        cells = grid;
        cellIndex = index;
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (paths == null || cells == null) return;
        foreach (var path in paths)
        {
            if (path == null || path.Cells == null || path.Cells.Length < 2) continue;
            if (System.Array.IndexOf(path.Cells, cellIndex) < 0) continue;
            var cellRect = cells[cellIndex/9, cellIndex%9].transform as RectTransform;
            Vector2 center = Center(path.Cells[0]);
            float size = Vector2.Distance(rectTransform.InverseTransformPoint(cellRect.TransformPoint(cellRect.rect.min)),
                rectTransform.InverseTransformPoint(cellRect.TransformPoint(new Vector2(cellRect.rect.xMax, cellRect.rect.yMin))));
            float radius = size * 0.29f;
            Color32 stroke = new Color(0.70f, 0.88f, 1f, 0.48f);
            for (int i = 1; i < path.Cells.Length; i++)
            {
                int previous = path.Cells[i-1], next = path.Cells[i];
                if (previous != cellIndex && next != cellIndex) continue;
                Vector2 a = Center(previous), b = Center(next);
                Vector2 midpoint = (a + b) * 0.5f;
                Vector2 direction = (b-a).normalized;
                if (previous == cellIndex)
                {
                    if (i == 1) a += direction * radius;
                    b = midpoint;
                }
                else a = midpoint;
                Vector2 normal = new Vector2(-direction.y, direction.x) * size * 0.065f;
                Quad(vh, a-normal, a+normal, b+normal, b-normal, stroke);
            }
            if (path.Cells[0] != cellIndex) continue;
            // The bulb belongs to the starting cell; digits render above it.
            const int segments = 32;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments, b = (i+1) * Mathf.PI * 2 / segments;
                Vector2 u = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), v = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Quad(vh, center+u*radius, center+v*radius, center+v*(radius-size*0.075f), center+u*(radius-size*0.075f), stroke);
            }
        }
    }
    private Vector2 Center(int index)
    {
        var cell = (RectTransform)cells[index/9,index%9].transform;
        return rectTransform.InverseTransformPoint(cell.TransformPoint(cell.rect.center));
    }
    private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 tint)
    {
        int index = vh.currentVertCount;
        vh.AddVert(a, tint, Vector2.zero); vh.AddVert(b, tint, Vector2.zero);
        vh.AddVert(c, tint, Vector2.zero); vh.AddVert(d, tint, Vector2.zero);
        vh.AddTriangle(index, index+1, index+2); vh.AddTriangle(index, index+2, index+3);
    }
}
