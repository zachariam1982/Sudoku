using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-authored edge Images draw the border in both edit and play mode.</summary>
public sealed class HomeTileBorder : Graphic
{
    [SerializeField] private RectTransform bottomEdge;

    protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();

    protected override void Start()
    {
        base.Start();
        // Keep the bottom stroke one screen pixel inside the tile to avoid clipping.
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        float inset = parentCanvas != null && parentCanvas.scaleFactor > 0f
            ? 1f / parentCanvas.scaleFactor
            : 1f;
        if (bottomEdge != null)
            bottomEdge.anchoredPosition = new Vector2(0f, inset);
    }
}
