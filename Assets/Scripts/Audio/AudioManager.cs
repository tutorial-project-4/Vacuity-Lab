using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource storySource;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    [Header("BGM")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float duckedBgmVolume = 0.45f;
    [SerializeField] private float defaultFadeDuration = 0.6f;

    private Coroutine bgmFadeRoutine;
    private float currentBgmTargetVolume;

    public bool HasBgm => bgmSource != null && bgmSource.clip != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[AudioManager] 씬에 AudioManager가 둘 이상 있습니다.", this);
            return;
        }

        Instance = this;
        CacheSources();
        ConfigureSources();
        currentBgmTargetVolume = bgmVolume;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayBgm(AudioClip clip)
    {
        PlayBgm(clip, defaultFadeDuration);
    }

    public void PlayBgm(AudioClip clip, float fadeDuration)
    {
        if (bgmSource == null || clip == null)
        {
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            FadeBgmTo(currentBgmTargetVolume, fadeDuration);
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = fadeDuration > 0f ? 0f : bgmVolume;
        bgmSource.Play();
        currentBgmTargetVolume = bgmVolume;
        FadeBgmTo(bgmVolume, fadeDuration);
    }

    public void StopBgm()
    {
        StopBgm(defaultFadeDuration);
    }

    public void StopBgm(float fadeDuration)
    {
        if (bgmSource == null)
        {
            return;
        }

        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
        }

        bgmFadeRoutine = StartCoroutine(StopBgmRoutine(Mathf.Max(0f, fadeDuration)));
    }

    public void DuckBgm(bool duck)
    {
        FadeBgmTo(duck ? duckedBgmVolume : bgmVolume, defaultFadeDuration);
    }

    public void PlaySfx(AudioClip clip)
    {
        PlayOneShot(sfxSource, clip, 1f);
    }

    public void PlaySfx(AudioClip clip, float volume)
    {
        PlayOneShot(sfxSource, clip, volume);
    }

    public void PlayUi(AudioClip clip)
    {
        PlayOneShot(uiSource, clip, 1f);
    }

    public void PlayStory(AudioClip clip)
    {
        PlayOneShot(storySource, clip, 1f);
    }

    public void PlaySfxAt(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
        }
    }

    private void FadeBgmTo(float targetVolume, float duration)
    {
        if (bgmSource == null)
        {
            return;
        }

        currentBgmTargetVolume = Mathf.Clamp01(targetVolume);
        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
        }

        bgmFadeRoutine = StartCoroutine(FadeBgmRoutine(bgmSource.volume, currentBgmTargetVolume, Mathf.Max(0f, duration)));
    }

    private IEnumerator FadeBgmRoutine(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            bgmSource.volume = to;
            bgmFadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        bgmSource.volume = to;
        bgmFadeRoutine = null;
    }

    private IEnumerator StopBgmRoutine(float duration)
    {
        yield return FadeBgmRoutine(bgmSource.volume, 0f, duration);
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    private static void PlayOneShot(AudioSource source, AudioClip clip, float volume)
    {
        if (source != null && clip != null)
        {
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }

    private void CacheSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        bgmSource = GetOrCreateSource(bgmSource, sources, 0);
        sfxSource = GetOrCreateSource(sfxSource, sources, 1);
        uiSource = GetOrCreateSource(uiSource, sources, 2);
        storySource = GetOrCreateSource(storySource, sources, 3);
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
        ConfigureSource(bgmSource, bgmGroup, true, 0f);
        ConfigureSource(sfxSource, sfxGroup, false, 0f);
        ConfigureSource(uiSource, uiGroup, false, 0f);
        ConfigureSource(storySource, uiGroup, false, 0f);
    }

    private static void ConfigureSource(AudioSource source, AudioMixerGroup mixerGroup, bool loop, float spatialBlend)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = spatialBlend;
        source.outputAudioMixerGroup = mixerGroup;
    }

    private void OnValidate()
    {
        bgmVolume = Mathf.Clamp01(bgmVolume);
        duckedBgmVolume = Mathf.Clamp01(duckedBgmVolume);
        defaultFadeDuration = Mathf.Max(0f, defaultFadeDuration);
        ConfigureSources();
    }
}
