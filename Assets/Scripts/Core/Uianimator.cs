using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable UI animation library for Bold & Playful Sudoku.
/// All methods return IEnumerator — run them via StartCoroutine().
/// </summary>
public static class UIAnimator
{
    // ── Easing functions ──────────────────────────────────────────────────────

    private static float EaseOut(float t)         => 1f - Mathf.Pow(1f - t, 3f);
    // ── Slide ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Slides a RectTransform in from an offset position while fading in.
    /// Use for picker appear animation.
    /// </summary>
    public static IEnumerator SlideIn(RectTransform target,
                                       Vector2 targetPos,
                                       float   slideOffset = 60f,
                                       float   duration    = 0.25f)
    {
        if (target == null) yield break;

        Vector2 startPos = targetPos + new Vector2(0f, -slideOffset);
        CanvasGroup cg   = GetOrAddCanvasGroup(target);

        cg.alpha             = 0f;
        target.anchoredPosition = startPos;
        target.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float e  = EaseOut(t);

            target.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, e);
            cg.alpha                = Mathf.Lerp(0f, 1f, e);
            yield return null;
        }

        target.anchoredPosition = targetPos;
        cg.alpha                = 1f;
    }

    /// <summary>
    /// Slides a RectTransform out downward while fading out, then deactivates it.
    /// Use for picker hide animation.
    /// </summary>
    public static IEnumerator SlideOut(RectTransform target,
                                        Vector2 fromPos,
                                        float   slideOffset = 40f,
                                        float   duration    = 0.15f)
    {
        if (target == null) yield break;

        Vector2    endPos = fromPos + new Vector2(0f, -slideOffset);
        CanvasGroup cg    = GetOrAddCanvasGroup(target);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float e  = EaseOut(t);

            target.anchoredPosition = Vector2.LerpUnclamped(fromPos, endPos, e);
            cg.alpha                = Mathf.Lerp(1f, 0f, e);
            yield return null;
        }

        target.gameObject.SetActive(false);
        cg.alpha = 1f; // reset for next show
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform rt)
    {
        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}
