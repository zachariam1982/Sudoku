using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable UI animation library for Bold & Playful Sudoku.
/// All methods return IEnumerator — run them via StartCoroutine().
/// </summary>
public static class UIAnimator
{
    // ── Easing functions ──────────────────────────────────────────────────────

    private static float EaseOut(float t)         => 1f - Mathf.Pow(1f - t, 3f);
    // ── Shake ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Horizontal shake. Use on wrong number entry.
    /// </summary>
    public static IEnumerator Shake(Transform target,
                                     Vector3 originalPosition,
                                     float magnitude = 18f,
                                     float duration  = 0.45f,
                                     int   vibrato   = 5)
    {
        float   elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress  = elapsed / duration;
            float dampen    = 1f - EaseOut(progress);   // gets weaker over time
            float offsetX   = Mathf.Sin(progress * Mathf.PI * vibrato * 2f)
                              * magnitude * dampen;
            target.localPosition = originalPosition + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }

        target.localPosition = originalPosition;
    }

    // ── Color Flash ───────────────────────────────────────────────────────────

    /// <summary>
    /// Flashes an Image to a color then fades back.
    /// Use on wrong number entry (flash red).
    /// </summary>
    public static IEnumerator Flash(Image image,
                                     Color flashColor,
                                     Color returnColor,
                                     float duration = 0.4f)
    {
        if (image == null) yield break;

        // Flash to error color instantly
        image.color = flashColor;

        // Fade back to return color
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed    += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / duration);
            image.color = Color.Lerp(flashColor, returnColor, EaseOut(t));
            yield return null;
        }

        image.color = returnColor;
    }

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

    // ── Wobble ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Small rotation wobble. Use on given cell tap (can't be edited feedback).
    /// </summary>
    public static IEnumerator Wobble(Transform target,
                                      Quaternion originalRotation,
                                      float angle    = 6f,
                                      float duration = 0.25f)
    {
        float      elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            float t   = elapsed / duration;
            float rot = Mathf.Sin(t * Mathf.PI * 3f) * angle * (1f - t);
            target.localRotation = originalRotation * Quaternion.Euler(0f, 0f, rot);
            yield return null;
        }

        target.localRotation = originalRotation;
    }

    // ── Overlay Fade ──────────────────────────────────────────────────────────

    // ── Helper ────────────────────────────────────────────────────────────────

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform rt)
    {
        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}
