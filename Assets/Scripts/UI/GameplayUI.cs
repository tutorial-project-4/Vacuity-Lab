using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameplayUI : MonoBehaviour
{
    const string VolumeKey = "ui.masterVolume";
    const string ShakeKey = "ui.screenShake";
    const string FullscreenKey = "ui.fullscreen";
    const string StartModeKey = "game.startMode";
    const string CheckpointKey = "game.checkpoint";
    const string QuickStartUnlockedKey = "game.quickStartUnlocked";

    [SerializeField] string titleScene = "Title-Consolidation";
    [SerializeField] GameObject optionsPrefab;
    [SerializeField] Sprite heartSprite;

    PlayerHealth health;
    PlayerWallPhaseDash wallDash;
    readonly List<Image> hearts = new();
    GameObject optionsPanel;
    GameObject deathPanel;
    OptionsPrefabUI optionsUI;
    IBossEncounter retryBoss;
    Transform retryPoint;
    float previousTimeScale = 1f;
    bool paused;

    void Awake()
    {
        health = FindFirstObjectByType<PlayerHealth>();
        wallDash = health ? health.GetComponent<PlayerWallPhaseDash>() : null;
        Build();
        ApplySavedOptions();
    }

    void Start()
    {
        if (PlayerPrefs.GetInt(StartModeKey, 0) != 1 || !health) return;

        if (PlayerPrefs.GetInt(CheckpointKey, 1) == 2)
        {
            Boss2IntroTrigger boss2Trigger = FindAnyObjectByType<Boss2IntroTrigger>();
            if (boss2Trigger != null && boss2Trigger.PrepareQuickStart(this))
            {
                health.Respawn(boss2Trigger.RetryPosition, health.MaxHearts);
                return;
            }
        }
        health.Respawn(BossEntrancePosition(), health.MaxHearts);
    }

    void OnEnable()
    {
        if (health)
        {
            health.HealthChanged += RefreshHearts;
            health.Died += ShowDeath;
            RefreshHearts(health.CurrentHearts, health.MaxHearts);
        }
    }

    void OnDisable()
    {
        if (health)
        {
            health.HealthChanged -= RefreshHearts;
            health.Died -= ShowDeath;
        }
        RestoreTime();
    }

    void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true
            && !deathPanel.activeSelf
            && !(DialogueRunner.Instance?.IsRunning ?? false))
            SetOptions(!optionsPanel.activeSelf);

    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();

        BuildPlayerHud();
        BuildBossHud();
        BuildOptions();
        BuildDeath();
    }

    void BuildPlayerHud()
    {
        var panel = Panel("Player HUD", transform, new Color(0.03f, .06f, .09f, .78f));
        Place(panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -34), new Vector2(320, 67), new Vector2(0, 1));

        var row = Rect("Hearts", panel.transform);
        Place(row, Vector2.zero, Vector2.zero, new Vector2(15, 11), new Vector2(370, 54), Vector2.zero);
        for (int i = 0; i < 7; i++)
        {
            var heart = Image("Heart", row.transform, new Color(.93f, .16f, .25f));
            heart.sprite = heartSprite;
            heart.preserveAspect = true;
            Place(heart.gameObject, Vector2.zero, Vector2.zero, new Vector2(i * 50, 0), new Vector2(42, 42), Vector2.zero);
            hearts.Add(heart);
        }
    }

    void BuildBossHud()
    {
        GameObject existing = GameObject.Find("BossHealthCanvas");
        if (!existing) return;
        var gauge = existing.transform.Find("BossGauge") as RectTransform;
        if (!gauge) return;
        gauge.anchoredPosition = new Vector2(0, -76);
        gauge.sizeDelta = new Vector2(760, 28);
    }

    void BuildOptions()
    {
        optionsPanel = Instantiate(optionsPrefab);
        optionsPanel.transform.localScale = Vector3.one;
        optionsUI = optionsPanel.AddComponent<OptionsPrefabUI>();
        optionsUI.Initialize(health, () => SetOptions(false), GoToTitle);
        optionsPanel.SetActive(false);
    }

    void BuildDeath()
    {
        deathPanel = Panel("Death Panel", transform, new Color(.015f, .02f, .03f, .94f));
        Stretch((RectTransform)deathPanel.transform, 0);
        var title = Label("Title", deathPanel.transform, "SYSTEM FAILURE", 58, TextAnchor.MiddleCenter);
        Place(title.gameObject, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 115), new Vector2(800, 80), new Vector2(.5f, .5f));
        ButtonControl(deathPanel.transform, "다시 시작", new Vector2(0, -10), Restart);
        ButtonControl(deathPanel.transform, "타이틀 화면", new Vector2(0, -100), GoToTitle);
        deathPanel.SetActive(false);
    }

    void RefreshHearts(int current, int max)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].gameObject.SetActive(i < max);
            hearts[i].color = i < current ? Color.white : new Color(.2f, .23f, .27f);
        }
    }

    void ShowDeath()
    {
        IBossEncounter boss = FindActiveBoss();
        if (boss != null && !boss.Health.IsDead)
        {
            PlayerPrefs.SetInt(QuickStartUnlockedKey, 1);
            PlayerPrefs.Save();
        }

        if (optionsPanel.activeSelf) SetOptions(false);
        deathPanel.SetActive(true);
        Pause();
        EventSystem.current?.SetSelectedGameObject(deathPanel.GetComponentInChildren<Button>().gameObject);
    }

    void SetOptions(bool visible)
    {
        optionsPanel.SetActive(visible);
        if (visible)
        {
            Pause();
            optionsUI.Show();
        }
        else
        {
            RestoreTime();
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }

    void Pause()
    {
        if (paused) return;
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0;
        paused = true;
    }

    void RestoreTime()
    {
        if (!paused) return;
        Time.timeScale = previousTimeScale;
        paused = false;
    }

    void Restart()
    {
        Time.timeScale = 1f;
        paused = false;

        IBossEncounter boss = retryBoss ?? FindActiveBoss();
        if (boss == null)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        deathPanel.SetActive(false);
        boss.ResetForRetry();
        wallDash?.ResetState();
        health?.Respawn(retryPoint ? (Vector2)retryPoint.position : BossEntrancePosition(), health.MaxHearts);
    }

    public void SetRetryCheckpoint(IBossEncounter boss, Transform point)
    {
        retryBoss = boss;
        retryPoint = point;
    }

    void GoToTitle()
    {
        Time.timeScale = 1f;
        paused = false;
        if (Application.CanStreamedLevelBeLoaded(titleScene)) SceneManager.LoadScene(titleScene);
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    Vector2 BossEntrancePosition()
    {
        var entrance = FindFirstObjectByType<BossBattleStartTrigger>();
        var trigger = entrance ? entrance.GetComponent<Collider2D>() : null;
        return trigger
            ? new Vector2(trigger.bounds.min.x - 1f, trigger.bounds.center.y)
            : health ? (Vector2)health.transform.position : Vector2.zero;
    }

    static IBossEncounter FindActiveBoss()
    {
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            if (behaviour is IBossEncounter boss && boss.IsBattleStarted && !boss.Health.IsDead) return boss;
        return null;
    }

    void ApplySavedOptions()
    {
        SetVolume(PlayerPrefs.GetFloat(VolumeKey, 1f));
        SetShake(PlayerPrefs.GetInt(ShakeKey, 1) != 0);
        SetFullscreen(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0);
    }

    void SetVolume(float value) { AudioListener.volume = value; PlayerPrefs.SetFloat(VolumeKey, value); }
    void SetShake(bool value) { CinemachineScreenShake2D.Enabled = value; PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0); }
    void SetFullscreen(bool value) { Screen.fullScreen = value; PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); }

    static GameObject Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject Panel(string name, Transform parent, Color color)
    {
        var go = Rect(name, parent);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static Image Image(string name, Transform parent, Color color)
    {
        var image = Rect(name, parent).AddComponent<Image>();
        image.color = color;
        return image;
    }

    static Text Label(string name, Transform parent, string value, int size, TextAnchor alignment)
    {
        var text = Rect(name, parent).AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.color = new Color(.88f, .94f, .97f);
        text.alignment = alignment;
        return text;
    }

    static void AddHeading(Transform parent, string value, float y)
    {
        var text = Label("Heading", parent, value, 42, TextAnchor.MiddleCenter);
        Place(text.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(550, 60), new Vector2(.5f, 1));
    }

    static Slider SliderControl(Transform parent, string label, float y)
    {
        var title = Label(label, parent, label, 23, TextAnchor.MiddleLeft);
        Place(title.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-220, y), new Vector2(220, 45), new Vector2(.5f, 1));
        var root = Rect(label + " Slider", parent);
        Place(root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(120, y - 4), new Vector2(300, 34), new Vector2(.5f, 1));
        var bg = Image("Background", root.transform, new Color(.12f, .18f, .22f)); Stretch(bg.rectTransform, 6);
        var fillArea = Rect("Fill Area", root.transform); Stretch((RectTransform)fillArea.transform, 8);
        var fill = Image("Fill", fillArea.transform, new Color(.1f, .75f, .84f)); Stretch(fill.rectTransform, 0);
        var handleArea = Rect("Handle Slide Area", root.transform); Stretch((RectTransform)handleArea.transform, 8);
        var handle = Image("Handle", handleArea.transform, Color.white); Place(handle.gameObject, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(22, 34), new Vector2(.5f, .5f));
        var slider = root.AddComponent<Slider>(); slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
        return slider;
    }

    static Toggle ToggleControl(Transform parent, string label, float y)
    {
        var root = Rect(label, parent);
        Place(root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(520, 48), new Vector2(.5f, 1));
        var bg = Image("Background", root.transform, new Color(.12f, .18f, .22f)); Place(bg.gameObject, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(42, 42), Vector2.zero);
        var check = Image("Checkmark", bg.transform, new Color(.1f, .75f, .84f)); Stretch(check.rectTransform, 8);
        var text = Label("Label", root.transform, label, 23, TextAnchor.MiddleLeft); Place(text.gameObject, Vector2.zero, Vector2.zero, new Vector2(58, 0), new Vector2(450, 42), Vector2.zero);
        var toggle = root.AddComponent<Toggle>(); toggle.targetGraphic = bg; toggle.graphic = check;
        return toggle;
    }

    static void ButtonControl(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var root = Panel(label + " Button", parent, new Color(.1f, .32f, .38f, 1));
        Place(root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(330, 66), new Vector2(.5f, .5f));
        var text = Label("Text", root.transform, label, 25, TextAnchor.MiddleCenter); Stretch(text.rectTransform, 0);
        var button = root.AddComponent<Button>(); button.targetGraphic = root.GetComponent<Image>(); button.onClick.AddListener(action);
    }

    static void Place(GameObject go, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Vector2 pivot)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot; rt.anchoredPosition = position; rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float margin)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.one * margin; rt.offsetMax = Vector2.one * -margin;
    }
}
