using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a hollow border without filling the button rectangle.
/// This is used by the home-screen tiles instead of Outline, which fills
/// transparent Images when Use Graphic Alpha is disabled.
/// </summary>
public sealed class HomeTileBorder : Graphic
{
    [SerializeField] private float thickness = 3f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var rect = rectTransform.rect;
        var halfWidth = rect.width * 0.5f;
        var halfHeight = rect.height * 0.5f;
        var t = Mathf.Clamp(thickness, 0.5f, Mathf.Min(rect.width, rect.height) * 0.5f);

        AddQuad(vh, new Rect(-halfWidth, halfHeight - t, rect.width, t));
        AddQuad(vh, new Rect(-halfWidth, -halfHeight, rect.width, t));
        AddQuad(vh, new Rect(-halfWidth, -halfHeight + t, t, rect.height - (2f * t)));
        AddQuad(vh, new Rect(halfWidth - t, -halfHeight + t, t, rect.height - (2f * t)));
    }

    private void AddQuad(VertexHelper vh, Rect r)
    {
        var start = vh.currentVertCount;
        var vertexColor = color;
        vh.AddVert(new Vector3(r.xMin, r.yMin), vertexColor, new Vector2(0f, 0f));
        vh.AddVert(new Vector3(r.xMin, r.yMax), vertexColor, new Vector2(0f, 1f));
        vh.AddVert(new Vector3(r.xMax, r.yMax), vertexColor, new Vector2(1f, 1f));
        vh.AddVert(new Vector3(r.xMax, r.yMin), vertexColor, new Vector2(1f, 0f));
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }
}
