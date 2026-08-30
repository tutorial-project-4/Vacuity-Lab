using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TitleAudioController : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Clips")]
    [SerializeField] private AudioClip titleBgmClip;
    [SerializeField] private AudioClip defaultHoverClip;
    [SerializeField] private AudioClip defaultClickClip;

    [Header("BGM")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.8f;
    [SerializeField] private float bgmFadeInDuration = 0.6f;

    private Coroutine bgmFadeRoutine;

    private void Awake()
    {
        CacheSources();
        ConfigureSources();
    }

    private void Start()
    {
        PlayTitleBgm();
    }

    public void PlayHover(AudioClip overrideClip = null)
    {
        PlayUi(overrideClip != null ? overrideClip : defaultHoverClip);
    }

    public void PlayClick(AudioClip overrideClip = null)
    {
        PlayUi(overrideClip != null ? overrideClip : defaultClickClip);
    }

    public void FadeOutTitleBgm(float duration)
    {
        if (bgmSource == null)
        {
            return;
        }

        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
        }

        bgmFadeRoutine = StartCoroutine(FadeOutBgmRoutine(Mathf.Max(0f, duration)));
    }

    private void PlayTitleBgm()
    {
        if (bgmSource == null || titleBgmClip == null)
        {
            return;
        }

        bgmSource.clip = titleBgmClip;
        bgmSource.loop = true;
        bgmSource.volume = bgmFadeInDuration > 0f ? 0f : bgmVolume;
        bgmSource.Play();

        if (bgmFadeInDuration > 0f)
        {
            bgmFadeRoutine = StartCoroutine(FadeBgmRoutine(0f, bgmVolume, bgmFadeInDuration, false));
        }
    }

    private IEnumerator FadeOutBgmRoutine(float duration)
    {
        yield return FadeBgmRoutine(bgmSource.volume, 0f, duration, true);
    }

    private IEnumerator FadeBgmRoutine(float from, float to, float duration, bool stopOnEnd)
    {
        if (duration <= 0f)
        {
            bgmSource.volume = to;
            if (stopOnEnd)
            {
                bgmSource.Stop();
            }

            bgmFadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        bgmSource.volume = to;
        if (stopOnEnd)
        {
            bgmSource.Stop();
        }

        bgmFadeRoutine = null;
    }

    private void PlayUi(AudioClip clip)
    {
        if (uiSource != null && clip != null)
        {
            uiSource.PlayOneShot(clip);
        }
    }

    private void CacheSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        bgmSource = GetOrCreateSource(bgmSource, sources, 0);
        uiSource = GetOrCreateSource(uiSource, sources, 1);
    }

    private AudioSource GetOrCreateSource(AudioSource source, AudioSource[] sources, int index)
    {
        if (source != null)
        {
            return source;
        }

        if (sources != null && sources.Length > index && sources[index] != null)
        {
            return sources[index];
        }

        return gameObject.AddComponent<AudioSource>();
    }

    private void ConfigureSources()
    {
        ConfigureSource(bgmSource, true);
        ConfigureSource(uiSource, false);
    }

    private static void ConfigureSource(AudioSource source, bool loop)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
    }

    private void OnValidate()
    {
        bgmVolume = Mathf.Clamp01(bgmVolume);
        bgmFadeInDuration = Mathf.Max(0f, bgmFadeInDuration);
        ConfigureSources();
    }
}
