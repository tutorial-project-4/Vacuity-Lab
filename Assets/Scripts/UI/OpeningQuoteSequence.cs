using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OpeningQuoteSequence : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private CanvasGroup quoteGroup;
    [SerializeField] private TMP_Text koreanQuoteText;
    [SerializeField] private TMP_Text englishQuoteText;
    [SerializeField] private TMP_Text authorText;
    [SerializeField] private TMP_FontAsset quoteFont;

    [Header("Text")]
    [SerializeField] private string koreanQuote = "망각은 고통스러운 기억으로부터 우리를 해방하는 열쇠다.";
    [SerializeField] private string englishQuote = "Forgetting is the key that frees us from painful memories.";
    [SerializeField] private string author = "— Sigmund Freud, attributed";

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField, Range(0f, 1f)] private float bgmTargetVolume = 0.8f;

    [Header("Timing")]
    [SerializeField] private float delayBeforeFadeIn = 0.25f;
    [SerializeField] private float fadeInDuration = 1.6f;
    [SerializeField] private float authorFadeInDelay = 0.35f;
    [SerializeField] private float authorFadeInDuration = 0.9f;
    [SerializeField] private float holdDuration = 2.6f;
    [SerializeField] private float fadeOutDuration = 1.2f;
    [SerializeField] private float sceneFadeInDuration = 1.2f;
    [SerializeField] private bool stopBgmOnSceneLoad = false;

    private Coroutine sequenceRoutine;
    public bool IsPlaying => sequenceRoutine != null;

    private void Awake()
    {
        EnsurePanel();
        ApplyText();
        HideInstant();

        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
        }

        ConfigureAudioSource();
    }

    public void Play(string nextSceneName)
    {
        if (sequenceRoutine != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("[OpeningQuoteSequence] Next scene name is empty.", this);
            return;
        }

        sequenceRoutine = StartCoroutine(SequenceRoutine(nextSceneName));
    }

    private IEnumerator SequenceRoutine(string nextSceneName)
    {
        EnsurePanel();
        if (quoteGroup == null)
        {
            Debug.LogWarning("[OpeningQuoteSequence] Quote panel is not ready. Loading scene immediately.", this);
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        ApplyText();
        SetQuoteAlpha(0f);
        SetAuthorAlpha(0f);

        quoteGroup.gameObject.SetActive(true);
        quoteGroup.alpha = 1f;
        quoteGroup.interactable = true;
        quoteGroup.blocksRaycasts = true;

        if (bgmSource != null && bgmSource.clip != null)
        {
            bgmSource.volume = 0f;
            bgmSource.Play();
        }

        if (delayBeforeFadeIn > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFadeIn);
        }

        Coroutine audioFadeIn = null;
        if (bgmSource != null && bgmSource.clip != null)
        {
            audioFadeIn = StartCoroutine(FadeAudioRoutine(0f, Mathf.Clamp01(bgmTargetVolume), fadeInDuration));
        }

        yield return FadeQuoteRoutine(0f, 1f, fadeInDuration);
        if (audioFadeIn != null)
        {
            yield return audioFadeIn;
        }

        if (authorFadeInDelay > 0f)
        {
            yield return new WaitForSeconds(authorFadeInDelay);
        }

        yield return FadeAuthorRoutine(0f, 1f, authorFadeInDuration);

        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }

        Coroutine audioFade = null;
        if (bgmSource != null && bgmSource.clip != null && stopBgmOnSceneLoad)
        {
            audioFade = StartCoroutine(FadeAudioRoutine(bgmSource.volume, 0f, fadeOutDuration));
        }

        yield return FadeTextRoutine(1f, 0f, fadeOutDuration);
        if (audioFade != null)
        {
            yield return audioFade;
        }

        quoteGroup.alpha = 1f;
        PersistTransitionObjects();
        SceneManager.LoadScene(nextSceneName);
        yield return null;

        yield return FadeRoutine(1f, 0f, sceneFadeInDuration);
        DestroyTransitionObjects();
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            quoteGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            quoteGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        quoteGroup.alpha = to;
    }

    private IEnumerator FadeAudioRoutine(float from, float to, float duration)
    {
        if (bgmSource == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            bgmSource.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bgmSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        bgmSource.volume = to;
    }

    private IEnumerator FadeTextRoutine(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetTextAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetTextAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetTextAlpha(to);
    }

    private IEnumerator FadeQuoteRoutine(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetQuoteAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetQuoteAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetQuoteAlpha(to);
    }

    private IEnumerator FadeAuthorRoutine(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAuthorAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAuthorAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAuthorAlpha(to);
    }

    private void EnsurePanel()
    {
        if (quoteGroup != null && koreanQuoteText != null && englishQuoteText != null && authorText != null)
        {
            return;
        }

        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        if (targetCanvas == null)
        {
#if UNITY_2023_1_OR_NEWER
            targetCanvas = FindFirstObjectByType<Canvas>();
#else
            targetCanvas = FindObjectOfType<Canvas>();
#endif
        }

        if (targetCanvas == null)
        {
            Debug.LogWarning("[OpeningQuoteSequence] Canvas was not found.", this);
            return;
        }

        GameObject panel = new GameObject("Opening Quote Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(targetCanvas.transform, false);
        RectTransform panelRect = (RectTransform)panel.transform;
        Stretch(panelRect, 0f);
        Image background = panel.GetComponent<Image>();
        background.color = Color.black;

        quoteGroup = panel.GetComponent<CanvasGroup>();

        koreanQuoteText = CreateText("Korean Quote Text", panel.transform, koreanQuote, 44, new Vector2(0f, 70f), new Vector2(1500f, 90f));
        englishQuoteText = CreateText("English Quote Text", panel.transform, englishQuote, 31, new Vector2(0f, -15f), new Vector2(1500f, 70f));
        authorText = CreateText("Author Text", panel.transform, author, 27, new Vector2(0f, -105f), new Vector2(1500f, 70f));
    }

    private void ConfigureAudioSource()
    {
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.playOnAwake = false;
    }

    private TMP_Text CreateText(string objectName, Transform parent, string text, int fontSize, Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }

    private void ApplyText()
    {
        if (koreanQuoteText != null)
        {
            ApplyFont(koreanQuoteText);
            koreanQuoteText.text = koreanQuote;
        }

        if (englishQuoteText != null)
        {
            ApplyFont(englishQuoteText);
            englishQuoteText.text = englishQuote;
        }

        if (authorText != null)
        {
            ApplyFont(authorText);
            authorText.text = author;
        }
    }

    private void ApplyFont(TMP_Text text)
    {
        if (quoteFont != null)
        {
            text.font = quoteFont;
        }
    }

    private void SetTextAlpha(float alpha)
    {
        SetQuoteAlpha(alpha);
        SetAuthorAlpha(alpha);
    }

    private void SetQuoteAlpha(float alpha)
    {
        SetTextAlpha(koreanQuoteText, alpha);
        SetTextAlpha(englishQuoteText, alpha);
    }

    private void SetAuthorAlpha(float alpha)
    {
        SetTextAlpha(authorText, alpha);
    }

    private static void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private void PersistTransitionObjects()
    {
        if (targetCanvas != null)
        {
            HideCanvasChildrenExceptQuotePanel();
            DontDestroyOnLoad(targetCanvas.gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void HideCanvasChildrenExceptQuotePanel()
    {
        if (targetCanvas == null || quoteGroup == null)
        {
            return;
        }

        Transform quotePanel = quoteGroup.transform;
        foreach (Transform child in targetCanvas.transform)
        {
            child.gameObject.SetActive(child == quotePanel);
        }
    }

    private void DestroyTransitionObjects()
    {
        if (targetCanvas != null)
        {
            Destroy(targetCanvas.gameObject);
        }

        Destroy(gameObject);
    }

    private void HideInstant()
    {
        if (quoteGroup == null)
        {
            return;
        }

        quoteGroup.alpha = 0f;
        quoteGroup.interactable = false;
        quoteGroup.blocksRaycasts = false;
        quoteGroup.gameObject.SetActive(false);
    }

    private static void Stretch(RectTransform rect, float margin)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * margin;
        rect.offsetMax = Vector2.one * -margin;
    }

    private void OnValidate()
    {
        delayBeforeFadeIn = Mathf.Max(0f, delayBeforeFadeIn);
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        authorFadeInDelay = Mathf.Max(0f, authorFadeInDelay);
        authorFadeInDuration = Mathf.Max(0f, authorFadeInDuration);
        holdDuration = Mathf.Max(0f, holdDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        sceneFadeInDuration = Mathf.Max(0f, sceneFadeInDuration);
        bgmTargetVolume = Mathf.Clamp01(bgmTargetVolume);

        if (bgmSource != null && !Application.isPlaying)
        {
            bgmSource.playOnAwake = false;
        }
    }
}
