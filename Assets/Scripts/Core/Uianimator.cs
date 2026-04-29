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
    private static float EaseOutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        float p = 0.3f;
        return Mathf.Pow(2f, -10f * t)
               * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
    }

    // ── Scale Punch ───────────────────────────────────────────────────────────

    /// <summary>
    /// Tap feedback: 1 → shrink → overshoot → settle.
    /// Use on empty cell tap.
    /// </summary>
    public static IEnumerator ScalePunch(Transform target,
                                          float shrink    = 0.88f,
                                          float overshoot = 1.14f,
                                          float duration  = 2.0f)
    {
        Vector3 original = target.localScale;

        // Phase 1 — shrink down
        float elapsed = 0f;
        float phase1  = duration * 0.35f;
        while (elapsed < phase1)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / phase1);
            target.localScale = Vector3.LerpUnclamped(original, original * shrink, EaseOut(t));
            Debug.Log($"[ScalePunch] Phase 1: t={t:F2} scale={target.localScale} and {elapsed:F2}s elapsed and {phase1:F2}s phase1");
            yield return null;
        }

        // Phase 2 — elastic overshoot back to original
        elapsed = 0f;
        float phase2 = duration * 0.65f;
        Vector3 from = target.localScale;
        while (elapsed < phase2)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / phase2);
            target.localScale = Vector3.LerpUnclamped(from, original, EaseOutElastic(t));
            yield return null;
        }

        target.localScale = original;
    }

    /// <summary>
    /// Number entry bounce: 1 → grow → settle.
    /// Use when a correct number is entered into a cell.
    /// </summary>
    public static IEnumerator ScaleBounce(Transform target,
                                           float peak     = 1.2f,
                                           float duration = 0.22f)
    {
        Vector3 original = target.localScale;

        // Phase 1 — grow
        float elapsed = 0f;
        float phase1  = duration * 0.4f;
        while (elapsed < phase1)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / phase1);
            target.localScale = Vector3.LerpUnclamped(original, original * peak, EaseOut(t));
            yield return null;
        }

        // Phase 2 — elastic settle
        elapsed = 0f;
        float phase2 = duration * 0.6f;
        Vector3 from = target.localScale;
        while (elapsed < phase2)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / phase2);
            target.localScale = Vector3.LerpUnclamped(from, original, EaseOutElastic(t));
            yield return null;
        }

        target.localScale = original;
    }

    // ── Shake ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Horizontal shake. Use on wrong number entry.
    /// </summary>
    public static IEnumerator Shake(Transform target,
                                     float magnitude = 18f,
                                     float duration  = 0.45f,
                                     int   vibrato   = 5)
    {
        Vector3 original = target.localPosition;
        float   elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress  = elapsed / duration;
            float dampen    = 1f - EaseOut(progress);   // gets weaker over time
            float offsetX   = Mathf.Sin(progress * Mathf.PI * vibrato * 2f)
                              * magnitude * dampen;
            target.localPosition = original + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }

        target.localPosition = original;
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

    /// <summary>
    /// Pulses between two colors multiple times — much more visible than a single flash.
    /// Use for error feedback on wrong number entry.
    /// </summary>
    public static IEnumerator Pulse(Image image,
                                    Color pulseColor,
                                    Color baseColor,
                                    int   pulseCount = 3,
                                    float pulseDuration = 0.15f)
    {
        if (image == null) yield break;

        for (int i = 0; i < pulseCount; i++)
        {
            // Flash to pulse color
            image.color = pulseColor;
            yield return new WaitForSeconds(pulseDuration * 0.4f);

            // Fade back to base
            float elapsed = 0f;
            float fadeDur = pulseDuration * 0.6f;
            while (elapsed < fadeDur)
            {
                elapsed    += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / fadeDur);
                image.color = Color.Lerp(pulseColor, baseColor, EaseOut(t));
                yield return null;
            }

            image.color = baseColor;
        }
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
                                      float angle    = 6f,
                                      float duration = 0.25f)
    {
        Quaternion original = target.localRotation;
        float      elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            float t   = elapsed / duration;
            float rot = Mathf.Sin(t * Mathf.PI * 3f) * angle * (1f - t);
            target.localRotation = original * Quaternion.Euler(0f, 0f, rot);
            yield return null;
        }

        target.localRotation = original;
    }

    // ── Overlay Fade ──────────────────────────────────────────────────────────

    /// <summary>
    /// Fades an Image alpha from 0 to target alpha.
    /// Use for overlay appear.
    /// </summary>
    public static IEnumerator FadeIn(Image image,
                                      float targetAlpha = 0.45f,
                                      float duration    = 0.2f)
    {
        if (image == null) yield break;

        Color c   = image.color;
        c.a       = 0f;
        image.color = c;
        image.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            c.a      = Mathf.Lerp(0f, targetAlpha, EaseOut(t));
            image.color = c;
            yield return null;
        }

        c.a         = targetAlpha;
        image.color = c;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform rt)
    {
        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}