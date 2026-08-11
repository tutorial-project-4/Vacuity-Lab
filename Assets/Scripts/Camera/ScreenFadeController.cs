using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFadeController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool startFadedOut;

    public float Alpha => canvasGroup != null ? canvasGroup.alpha : 0f;

    private void Awake()
    {
        CacheComponents();
        SetAlpha(startFadedOut ? 1f : 0f);
    }

    public IEnumerator FadeOut(float duration)
    {
        yield return FadeTo(1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return FadeTo(0f, duration);
    }

    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        CacheComponents();

        if (canvasGroup == null)
        {
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        targetAlpha = Mathf.Clamp01(targetAlpha);
        duration = Mathf.Max(0f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    public void SetAlpha(float alpha)
    {
        CacheComponents();

        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = Mathf.Clamp01(alpha);
        canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.001f;
        canvasGroup.interactable = false;
    }

    private void CacheComponents()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
