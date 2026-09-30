using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a hollow border around a home-screen tile.
/// In Play mode the four edges use separate UI Images, avoiding fragile
/// custom-mesh clipping when the responsive layout changes on rotation.
/// </summary>
public sealed class HomeTileBorder : Graphic
{
    [SerializeField] private float thickness = 5f;

    private static readonly string[] EdgeNames =
    {
        "__BorderTop",
        "__BorderBottom",
        "__BorderLeft",
        "__BorderRight"
    };

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        // Preserve the editor preview. Runtime uses four independent Image
        // renderers because the top mesh edge was clipped after orientation changes.
        if (Application.isPlaying)
            return;

        var rect = rectTransform.rect;
        var halfWidth = rect.width * 0.5f;
        var halfHeight = rect.height * 0.5f;
        var t = Mathf.Clamp(thickness, 0.5f, Mathf.Min(rect.width, rect.height) * 0.5f);

        AddQuad(vh, new Rect(-halfWidth, halfHeight - t, rect.width, t));
        AddQuad(vh, new Rect(-halfWidth, -halfHeight, rect.width, t));
        AddQuad(vh, new Rect(-halfWidth, -halfHeight + t, t, rect.height - (2f * t)));
        AddQuad(vh, new Rect(halfWidth - t, -halfHeight + t, t, rect.height - (2f * t)));
    }

    private void Start()
    {
        // The button resizes when HomeScreenController switches orientation.
        // Anchored edge Images track that RectTransform without a mesh rebuild.
        CreateEdge(EdgeNames[0], new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, thickness));
        CreateEdge(EdgeNames[1], new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, thickness));
        CreateEdge(EdgeNames[2], new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(thickness, 0f));
        CreateEdge(EdgeNames[3], new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(1f, 0.5f), new Vector2(thickness, 0f));
    }

    private void CreateEdge(
        string edgeName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta)
    {
        Transform existing = transform.Find(edgeName);
        RectTransform edgeTransform;
        Image edgeImage;

        if (existing == null)
        {
            var edgeObject = new GameObject(
                edgeName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            edgeTransform = edgeObject.GetComponent<RectTransform>();
            edgeTransform.SetParent(transform, false);
            edgeImage = edgeObject.GetComponent<Image>();
        }
        else
        {
            edgeTransform = existing as RectTransform;
            edgeImage = existing.GetComponent<Image>();
            if (edgeImage == null)
                edgeImage = existing.gameObject.AddComponent<Image>();
        }

        edgeTransform.anchorMin = anchorMin;
        edgeTransform.anchorMax = anchorMax;
        edgeTransform.pivot = pivot;
        edgeTransform.anchoredPosition = Vector2.zero;
        edgeTransform.sizeDelta = sizeDelta;

        edgeImage.sprite = null;
        edgeImage.color = color;
        edgeImage.raycastTarget = false;
        edgeImage.maskable = true;
        edgeTransform.SetAsLastSibling();
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
