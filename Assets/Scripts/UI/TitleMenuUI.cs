using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TitleMenuUI : MonoBehaviour
{
    [SerializeField] string gameScene = "boss-semi-complete-arena";
    const string StartModeKey = "game.startMode";
    const string VolumeKey = "ui.masterVolume";
    const string ShakeKey = "ui.screenShake";
    const string FullscreenKey = "ui.fullscreen";
    const string QuickStartUnlockedKey = "game.quickStartUnlocked";

    GameObject mainPanel;
    GameObject optionsPanel;
    Button firstButton;
    Button quickStartButton;

    void Awake()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();
        Build();
        ApplySavedOptions();
    }

    void Start() => EventSystem.current?.SetSelectedGameObject(firstButton.gameObject);

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();

        var background = Rect("Background", transform).AddComponent<RawImage>();
        background.texture = Resources.Load<Texture2D>("UI/title-background");
        background.color = Color.white;
        Stretch(background.rectTransform, 0);
        var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;

        var shade = Rect("Shade", transform).AddComponent<Image>();
        shade.color = new Color(0, 0, 0, .18f);
        Stretch(shade.rectTransform, 0);

        mainPanel = Rect("Main Menu", transform);
        Stretch((RectTransform)mainPanel.transform, 0);
        BuildLogo(mainPanel.transform);
        firstButton = MenuButton(mainPanel.transform, "게임 시작", -455, () => StartGame(0));
        quickStartButton = MenuButton(mainPanel.transform, "빠른 시작", -525, () => StartGame(1));
        quickStartButton.interactable = PlayerPrefs.GetInt(QuickStartUnlockedKey, 0) != 0;
        if (!quickStartButton.interactable)
            quickStartButton.GetComponentInChildren<Text>().text = "빠른 시작  ·  잠김";
        MenuButton(mainPanel.transform, "OPTIONS", -595, OpenOptions);
        MenuButton(mainPanel.transform, "END", -665, Quit);

        BuildOptions();
    }

    void BuildLogo(Transform parent)
    {
        var title = Label("Logo", parent, "VACUITY LAB", 92, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Normal;
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 48;
        title.resizeTextMaxSize = 92;
        Place(title.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -170), new Vector2(920, 130), new Vector2(.5f, 1));

        var subtitle = Label("Subtitle", parent, "DESCEND  ·  REMEMBER  ·  ESCAPE", 18, TextAnchor.MiddleCenter);
        subtitle.color = new Color(.66f, .79f, .83f);
        Place(subtitle.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -300), new Vector2(700, 32), new Vector2(.5f, 1));

        Line(parent, new Vector2(-280, -330));
        Line(parent, new Vector2(280, -330));
        var diamond = Rect("Mark", parent).AddComponent<Image>();
        diamond.color = new Color(.72f, .88f, .91f, .8f);
        Place(diamond.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -330), new Vector2(12, 12), new Vector2(.5f, .5f));
        diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
    }

    void BuildOptions()
    {
        optionsPanel = Rect("Options", transform);
        Stretch((RectTransform)optionsPanel.transform, 0);
        var dim = optionsPanel.AddComponent<Image>();
        dim.color = new Color(.01f, .025f, .035f, .94f);

        var heading = Label("Heading", optionsPanel.transform, "OPTIONS", 52, TextAnchor.MiddleCenter);
        Place(heading.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -150), new Vector2(600, 80), new Vector2(.5f, 1));

        var volume = SliderControl(optionsPanel.transform, "마스터 볼륨", -335);
        volume.value = PlayerPrefs.GetFloat(VolumeKey, 1f);
        volume.onValueChanged.AddListener(SetVolume);
        var shake = ToggleControl(optionsPanel.transform, "화면 흔들림", -440);
        shake.isOn = PlayerPrefs.GetInt(ShakeKey, 1) != 0;
        shake.onValueChanged.AddListener(SetShake);
        var fullscreen = ToggleControl(optionsPanel.transform, "전체 화면", -515);
        fullscreen.isOn = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
        fullscreen.onValueChanged.AddListener(SetFullscreen);
        MenuButton(optionsPanel.transform, "BACK", -650, CloseOptions);
        optionsPanel.SetActive(false);
    }

    void StartGame(int mode)
    {
        PlayerPrefs.SetInt(StartModeKey, mode);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameScene);
    }

    void OpenOptions() { mainPanel.SetActive(false); optionsPanel.SetActive(true); EventSystem.current?.SetSelectedGameObject(optionsPanel.GetComponentInChildren<Button>().gameObject); }
    void CloseOptions() { optionsPanel.SetActive(false); mainPanel.SetActive(true); EventSystem.current?.SetSelectedGameObject(firstButton.gameObject); }
    void Quit() { PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ApplySavedOptions()
    {
        SetVolume(PlayerPrefs.GetFloat(VolumeKey, 1f));
        SetShake(PlayerPrefs.GetInt(ShakeKey, 1) != 0);
        SetFullscreen(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0);
    }

    static void SetVolume(float value) { AudioListener.volume = value; PlayerPrefs.SetFloat(VolumeKey, value); }
    static void SetShake(bool value) { CinemachineScreenShake2D.Enabled = value; PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0); }
    static void SetFullscreen(bool value) { Screen.fullScreen = value; PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); }

    static void EnsureEventSystem()
    {
        if (EventSystem.current) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        EventSystem.current = go.GetComponent<EventSystem>();
    }

    static Button MenuButton(Transform parent, string value, float y, UnityEngine.Events.UnityAction action)
    {
        var go = Rect(value + " Button", parent);
        Place(go, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(400, 54), new Vector2(.5f, 1));
        var text = Label("Text", go.transform, value, 26, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 0);
        var image = go.AddComponent<Image>();
        image.color = new Color(.12f, .25f, .29f, 0);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(1, 1, 1, 0);
        colors.highlightedColor = new Color(.25f, .72f, .8f, .42f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.2f, .8f, .9f, .65f);
        button.colors = colors;
        button.onClick.AddListener(action);
        return button;
    }

    static Slider SliderControl(Transform parent, string label, float y)
    {
        var text = Label(label, parent, label, 24, TextAnchor.MiddleLeft);
        Place(text.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-250, y), new Vector2(260, 46), new Vector2(.5f, 1));
        var root = Rect(label + " Slider", parent);
        Place(root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(120, y - 5), new Vector2(330, 36), new Vector2(.5f, 1));
        var bg = Rect("Background", root.transform).AddComponent<Image>(); bg.color = new Color(.12f, .2f, .23f); Stretch(bg.rectTransform, 8);
        var fillArea = Rect("Fill Area", root.transform); Stretch((RectTransform)fillArea.transform, 10);
        var fill = Rect("Fill", fillArea.transform).AddComponent<Image>(); fill.color = new Color(.35f, .78f, .84f); Stretch(fill.rectTransform, 0);
        var handleArea = Rect("Handle Slide Area", root.transform); Stretch((RectTransform)handleArea.transform, 10);
        var handle = Rect("Handle", handleArea.transform).AddComponent<Image>(); handle.color = Color.white; Place(handle.gameObject, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20, 34), new Vector2(.5f, .5f));
        var slider = root.AddComponent<Slider>(); slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
        return slider;
    }

    static Toggle ToggleControl(Transform parent, string label, float y)
    {
        var root = Rect(label, parent);
        Place(root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(560, 48), new Vector2(.5f, 1));
        var bg = Rect("Background", root.transform).AddComponent<Image>(); bg.color = new Color(.12f, .2f, .23f); Place(bg.gameObject, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(42, 42), Vector2.zero);
        var check = Rect("Checkmark", bg.transform).AddComponent<Image>(); check.color = new Color(.35f, .78f, .84f); Stretch(check.rectTransform, 8);
        var text = Label("Label", root.transform, label, 24, TextAnchor.MiddleLeft); Place(text.gameObject, Vector2.zero, Vector2.zero, new Vector2(62, 0), new Vector2(480, 42), Vector2.zero);
        var toggle = root.AddComponent<Toggle>(); toggle.targetGraphic = bg; toggle.graphic = check;
        return toggle;
    }

    static void Line(Transform parent, Vector2 position)
    {
        var line = Rect("Line", parent).AddComponent<Image>();
        line.color = new Color(.55f, .75f, .79f, .45f);
        Place(line.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), position, new Vector2(240, 2), new Vector2(.5f, .5f));
    }

    static GameObject Rect(string name, Transform parent) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go; }
    static Text Label(string name, Transform parent, string value, int size, TextAnchor alignment) { var text = Rect(name, parent).AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value; text.fontSize = size; text.color = new Color(.9f, .95f, .97f); text.alignment = alignment; return text; }
    static void Place(GameObject go, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Vector2 pivot) { var rt = (RectTransform)go.transform; rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot; rt.anchoredPosition = position; rt.sizeDelta = size; }
    static void Stretch(RectTransform rt, float margin) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.one * margin; rt.offsetMax = Vector2.one * -margin; }

#if UNITY_EDITOR
    [ContextMenu("Test: Unlock Quick Start")]
    void UnlockQuickStart() { PlayerPrefs.SetInt(QuickStartUnlockedKey, 1); PlayerPrefs.Save(); }

    [ContextMenu("Test: Lock Quick Start")]
    void LockQuickStart() { PlayerPrefs.DeleteKey(QuickStartUnlockedKey); PlayerPrefs.Save(); }
#endif
}
