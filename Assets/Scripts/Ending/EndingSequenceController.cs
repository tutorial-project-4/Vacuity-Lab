using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public struct EndingTextLine
{
    public string speaker;
    [TextArea] public string text;
}

[Serializable]
public struct EndingSlide
{
    public string title;
    public Sprite image;
    [TextArea] public string text;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class EndingSequenceController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Key[] advanceKeys = { Key.F, Key.Space, Key.Enter };

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image slideImage;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip endingBgm;
    [SerializeField] private AudioClip fadeSfx;
    [SerializeField] private AudioClip pageSfx;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.8f;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "Title 1";

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    [SerializeField] private float charactersPerSecond = 35f;
    [SerializeField] private float slideFadeDuration = 0.35f;
    [SerializeField] private bool waitForInputAfterEachLine = true;

    [Header("Black Screen Dialogue")]
    [SerializeField] private EndingTextLine[] blackScreenLines =
    {
        new EndingTextLine { speaker = "대니", text = "탈출이다! 크으윽, 몇 년 만의 상쾌한 바깥공기냐!" },
        new EndingTextLine { speaker = "폴", text = "온통 풀이군." }
    };

    [Header("Slides")]
    [SerializeField] private EndingSlide[] slides =
    {
        new EndingSlide
        {
            title = "그림 1) 풀 숲",
            text = "우리는 무작정 풀을 헤치고 걸었다. 도로가 나올 때까지.\n한참 걷고 나서 마침내 도로를 발견한 우리는 간신히 풀만 가득한 이 산을 벗어났다."
        },
        new EndingSlide
        {
            title = "그림 2) 모텔",
            text = "근처 모텔에 도착한 후 대니는 이곳에 있으면 누군가 나를 데리러 올 것이라 일러주고 사라졌다. 아무것도 기억나지 않았지만 주사를 한 대 맞으니 기분이 괜찮아진다. 다행히 그 친절한 남자는 떠나면서 나에게 여러 개의 주사기를 주고 갔다."
        },
        new EndingSlide
        {
            title = "그림 3) 텔레비전",
            text = "얼마나 시간이 지났을까, 기억을 회복할 때마다 주사를 놓으니 시간의 흐름도 알 수 없어졌다. 갖고 있던 주사기는 모두 사용했다. 조금씩 기억이 돌아오는 감각이 불쾌하다. 오래된 모텔 TV에서 내 얼굴의 수배지가 방영되고 있다.\n[ Q 연구소 부소장 폴 맥그래스 ]\n연구원인 아내와 연구소장을 살해하고 연구소를 폭파, 현재는 도주 중... 아나운서가 심각한 목소리로 말한다. 도대체 영문을 모르겠는 소리 뿐이다. 기억이 조금씩 돌아오고 있다. 아주 불쾌하고 역겨운 기분이다. 주사기, 주사기가 필요하다.\n[end1 주사기가 필요해]"
        }
    };

    private Coroutine routine;
    private PlayerController lockedPlayer;

    public bool IsPlaying => routine != null;

    private void Awake()
    {
        EnsureAdvanceKeys();
        CacheReferences();
        SetVisible(false);
    }

    public bool Play(PlayerController player)
    {
        if (routine != null)
        {
            return false;
        }

        lockedPlayer = player;
        routine = StartCoroutine(SequenceRoutine());
        return true;
    }

    private IEnumerator SequenceRoutine()
    {
        CacheReferences();
        if (rootGroup == null || bodyText == null)
        {
            Debug.LogWarning("[EndingSequenceController] Ending UI references are missing.", this);
            routine = null;
            yield break;
        }

        lockedPlayer?.SetCutsceneLock(true);
        PlayBgm();
        PlaySfx(fadeSfx);

        SetVisible(true);
        SetSlideVisible(false, 0f);
        ClearText();
        yield return FadeCanvas(0f, 1f, fadeInDuration);

        for (int i = 0; i < blackScreenLines.Length; i++)
        {
            yield return TypeLine(blackScreenLines[i].speaker, blackScreenLines[i].text);
            if (waitForInputAfterEachLine)
            {
                PlaySfx(pageSfx);
                yield return WaitForAdvance();
            }
        }

        for (int i = 0; i < slides.Length; i++)
        {
            PlaySfx(pageSfx);
            yield return ShowSlide(slides[i]);
        }

        PlaySfx(fadeSfx);
        ClearText();
        SetSlideVisible(false, 0f);
        if (fadeOutDuration > 0f)
        {
            yield return new WaitForSeconds(fadeOutDuration);
        }

        LoadTitleScene();
    }

    private IEnumerator ShowSlide(EndingSlide slide)
    {
        if (slideImage != null)
        {
            slideImage.sprite = slide.image;
            slideImage.preserveAspect = true;
            SetSlideVisible(slide.image != null, 0f);
            yield return FadeSlide(0f, slide.image != null ? 1f : 0f, slideFadeDuration);
        }

        string[] lines = SplitSlideText(slide.text);
        if (lines.Length == 0)
        {
            yield return TypeLine(slide.title, string.Empty);
            yield return WaitForAdvance();
            yield break;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            yield return TypeLine(slide.title, lines[i]);
            yield return WaitForAdvance();
        }
    }

    private IEnumerator TypeLine(string speaker, string text)
    {
        if (speakerText != null)
        {
            speakerText.gameObject.SetActive(!string.IsNullOrWhiteSpace(speaker));
            speakerText.text = speaker;
        }

        string fullText = text ?? string.Empty;
        bodyText.text = string.Empty;

        float interval = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;
        for (int i = 0; i < fullText.Length; i++)
        {
            if (WasAdvancePressed())
            {
                bodyText.text = fullText;
                yield return null;
                yield break;
            }

            bodyText.text += fullText[i];
            if (interval > 0f)
            {
                yield return new WaitForSeconds(interval);
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator WaitForAdvance()
    {
        yield return null;
        while (!WasAdvancePressed())
        {
            yield return null;
        }
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        if (rootGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            rootGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rootGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        rootGroup.alpha = to;
    }

    private IEnumerator FadeSlide(float from, float to, float duration)
    {
        if (slideImage == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            SetSlideAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetSlideAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetSlideAlpha(to);
    }

    private bool WasAdvancePressed()
    {
        EnsureAdvanceKeys();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        for (int i = 0; i < advanceKeys.Length; i++)
        {
            if (keyboard[advanceKeys[i]].wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureAdvanceKeys()
    {
        if (advanceKeys == null || advanceKeys.Length == 0)
        {
            advanceKeys = new[] { Key.F, Key.Space, Key.Enter };
        }
    }

    private static string[] SplitSlideText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        List<string> lines = new List<string>();
        int start = 0;

        for (int i = 0; i < normalized.Length; i++)
        {
            char current = normalized[i];
            bool isBreak = current == '\n' || current == '.' || current == '!' || current == '?' || current == '…';
            if (!isBreak)
            {
                continue;
            }

            int end = i + 1;
            while (end < normalized.Length && normalized[end] == current && current != '\n')
            {
                end++;
                i++;
            }

            AddSlideLine(lines, normalized.Substring(start, end - start));
            start = end;

            while (start < normalized.Length && char.IsWhiteSpace(normalized[start]))
            {
                start++;
                i = start - 1;
            }
        }

        if (start < normalized.Length)
        {
            AddSlideLine(lines, normalized.Substring(start));
        }

        return lines.ToArray();
    }

    private static void AddSlideLine(List<string> lines, string text)
    {
        string trimmed = text.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            lines.Add(trimmed);
        }
    }

    private void LoadTitleScene()
    {
        if (!string.IsNullOrWhiteSpace(titleSceneName) && Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
            return;
        }

        Debug.LogWarning($"[EndingSequenceController] Title scene '{titleSceneName}' is not in build settings. Reloading the active scene.", this);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void PlayBgm()
    {
        if (bgmSource == null || endingBgm == null)
        {
            return;
        }

        bgmSource.clip = endingBgm;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void CacheReferences()
    {
        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>(true);
        }

        if (canvas == null)
        {
            BuildRuntimeUi();
        }

        if (rootGroup == null && canvas != null)
        {
            rootGroup = canvas.GetComponentInChildren<CanvasGroup>(true);
        }

        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = bgmSource;
        }
    }

    private void BuildRuntimeUi()
    {
        GameObject canvasObject = new GameObject("Ending Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        Stretch((RectTransform)canvasObject.transform);

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject screenObject = new GameObject("Ending Screen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        screenObject.transform.SetParent(canvasObject.transform, false);
        Stretch((RectTransform)screenObject.transform);

        backgroundImage = screenObject.GetComponent<Image>();
        backgroundImage.color = Color.black;
        backgroundImage.raycastTarget = true;
        rootGroup = screenObject.GetComponent<CanvasGroup>();

        GameObject imageObject = new GameObject("Ending Slide Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(screenObject.transform, false);
        RectTransform imageRect = (RectTransform)imageObject.transform;
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = new Vector2(0f, 115f);
        imageRect.sizeDelta = new Vector2(1320f, 742f);
        slideImage = imageObject.GetComponent<Image>();
        slideImage.preserveAspect = true;
        slideImage.raycastTarget = false;

        speakerText = CreateRuntimeText("Ending Speaker Text", screenObject.transform, new Vector2(0f, 262f), new Vector2(1420f, 48f), 30, TextAnchor.MiddleLeft);
        bodyText = CreateRuntimeText("Ending Body Text", screenObject.transform, new Vector2(0f, 58f), new Vector2(1420f, 210f), 28, TextAnchor.UpperLeft);
    }

    private static Text CreateRuntimeText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void SetVisible(bool visible)
    {
        if (rootGroup == null)
        {
            return;
        }

        rootGroup.gameObject.SetActive(visible);
        rootGroup.alpha = visible ? rootGroup.alpha : 0f;
        rootGroup.interactable = visible;
        rootGroup.blocksRaycasts = visible;
    }

    private void SetSlideVisible(bool visible, float alpha)
    {
        if (slideImage == null)
        {
            return;
        }

        slideImage.gameObject.SetActive(visible);
        SetSlideAlpha(alpha);
    }

    private void SetSlideAlpha(float alpha)
    {
        if (slideImage == null)
        {
            return;
        }

        Color color = slideImage.color;
        color.a = alpha;
        slideImage.color = color;
    }

    private void ClearText()
    {
        if (speakerText != null)
        {
            speakerText.text = string.Empty;
            speakerText.gameObject.SetActive(false);
        }

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        lockedPlayer?.SetCutsceneLock(false);
        lockedPlayer = null;
    }

    private void OnValidate()
    {
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        charactersPerSecond = Mathf.Max(0f, charactersPerSecond);
        slideFadeDuration = Mathf.Max(0f, slideFadeDuration);
        bgmVolume = Mathf.Clamp01(bgmVolume);
    }
}
