using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TitleMenuUI : MonoBehaviour
{
    [SerializeField] string gameScene = "boss-semi-complete-arena";
    [SerializeField] Button newGameButton;
    [SerializeField] Button quickStartButton;
    [SerializeField] Button optionButton;
    [SerializeField] Button quitButton;
    [SerializeField] GameObject optionsPrefab;
    const string StartModeKey = "game.startMode";
    const string CheckpointKey = "game.checkpoint";
    const string VolumeKey = "ui.masterVolume";
    const string ShakeKey = "ui.screenShake";
    const string FullscreenKey = "ui.fullscreen";
    const string QuickStartUnlockedKey = "game.quickStartUnlocked";

    GameObject optionsPanel;
    OptionsPrefabUI optionsUI;

    void Awake()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();
        newGameButton.onClick.AddListener(() => StartGame(0));
        quickStartButton.onClick.AddListener(() => StartGame(1));
        optionButton.onClick.AddListener(OpenOptions);
        quitButton.onClick.AddListener(Quit);
        quickStartButton.interactable = PlayerPrefs.GetInt(QuickStartUnlockedKey, 0) != 0;
        BuildOptions();
        ApplySavedOptions();
    }

    void Start() => EventSystem.current?.SetSelectedGameObject(newGameButton.gameObject);

    void BuildOptions()
    {
        optionsPanel = Instantiate(optionsPrefab);
        optionsPanel.transform.localScale = Vector3.one;
        optionsUI = optionsPanel.AddComponent<OptionsPrefabUI>();
        optionsUI.Initialize(null, CloseOptions, CloseOptions);
        optionsPanel.SetActive(false);
    }

    void StartGame(int mode)
    {
        PlayerPrefs.SetInt(StartModeKey, mode);
        if (mode == 0) PlayerPrefs.SetInt(CheckpointKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameScene);
    }

    void OpenOptions() { optionsPanel.SetActive(true); optionsUI.Show(); }
    void CloseOptions() { optionsPanel.SetActive(false); EventSystem.current?.SetSelectedGameObject(newGameButton.gameObject); }
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
