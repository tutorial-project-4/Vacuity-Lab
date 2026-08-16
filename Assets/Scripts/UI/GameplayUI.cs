using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameplayUI : MonoBehaviour
{
    const string VolumeKey = "ui.masterVolume";
    const string ShakeKey = "ui.screenShake";
    const string FullscreenKey = "ui.fullscreen";

    PlayerHealth health;
    PlayerMovement movement;
    PlayerWallPhaseDash wallDash;
    BossHealth bossHealth;
    Boss boss;
    readonly List<Image> hearts = new();
    Image dashCooldown;
    Image wallDashCooldown;
    Text memoryText;
    GameObject optionsPanel;
    GameObject deathPanel;
    float previousTimeScale = 1f;
    bool paused;

    void Awake()
    {
        health = FindFirstObjectByType<PlayerHealth>();
        movement = health ? health.GetComponent<PlayerMovement>() : null;
        wallDash = health ? health.GetComponent<PlayerWallPhaseDash>() : null;
        bossHealth = FindFirstObjectByType<BossHealth>();
        boss = bossHealth ? bossHealth.GetComponent<Boss>() : null;
        Build();
        ApplySavedOptions();
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
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && !deathPanel.activeSelf)
            SetOptions(!optionsPanel.activeSelf);

        if (dashCooldown) dashCooldown.fillAmount = movement ? movement.DashCooldownRatio : 0f;
        if (wallDashCooldown) wallDashCooldown.fillAmount = wallDash ? wallDash.CooldownRatio : 0f;
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
        BuildSkillHud();
        BuildBossHud();
        BuildOptions();
        BuildDeath();
    }

    void BuildPlayerHud()
    {
        var panel = Panel("Player HUD", transform, new Color(0.03f, .06f, .09f, .78f));
        Place(panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -34), new Vector2(410, 132), new Vector2(0, 1));

        var row = Rect("Hearts", panel.transform);
        Place(row, Vector2.zero, Vector2.zero, new Vector2(18, 60), new Vector2(370, 54), Vector2.zero);
        for (int i = 0; i < 7; i++)
        {
            var heart = Image("Heart", row.transform, new Color(.93f, .16f, .25f));
            Place(heart.gameObject, Vector2.zero, Vector2.zero, new Vector2(i * 50, 0), new Vector2(42, 42), Vector2.zero);
            hearts.Add(heart);
        }

        memoryText = Label("Memory Counter", panel.transform, "회수된 기억  0 / 3", 25, TextAnchor.MiddleLeft);
        Place(memoryText.gameObject, Vector2.zero, Vector2.zero, new Vector2(18, 14), new Vector2(370, 36), Vector2.zero);
    }

    void BuildSkillHud()
    {
        var panel = Panel("Skill HUD", transform, new Color(0.03f, .06f, .09f, .78f));
        Place(panel, Vector2.zero, Vector2.zero, new Vector2(34, 34), new Vector2(310, 110), Vector2.zero);
        dashCooldown = Skill(panel.transform, "SHIFT", "DASH", 16);
        wallDashCooldown = Skill(panel.transform, "E", "PHASE", 156);
    }

    Image Skill(Transform parent, string key, string title, float x)
    {
        var root = Panel(title, parent, new Color(.12f, .2f, .25f, 1));
        Place(root, Vector2.zero, Vector2.zero, new Vector2(x, 14), new Vector2(128, 80), Vector2.zero);
        var text = Label("Label", root.transform, $"{title}\n[{key}]", 18, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 5);
        var overlay = Image("Cooldown", root.transform, new Color(0, 0, 0, .72f));
        Stretch(overlay.rectTransform, 0);
        overlay.type = UnityEngine.UI.Image.Type.Filled;
        overlay.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
        overlay.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Top;
        return overlay;
    }

    void BuildBossHud()
    {
        GameObject existing = GameObject.Find("BossHealthCanvas");
        if (!existing) return;
        var gauge = existing.transform.Find("BossGauge") as RectTransform;
        if (!gauge) return;
        gauge.anchoredPosition = new Vector2(0, -76);
        gauge.sizeDelta = new Vector2(760, 28);
        var name = Label("Boss Name", existing.transform, "WARDEN-01", 30, TextAnchor.MiddleCenter);
        Place(name.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(760, 38), new Vector2(.5f, 1));
    }

    void BuildOptions()
    {
        optionsPanel = Panel("Options Panel", transform, new Color(.02f, .035f, .055f, .97f));
        Place(optionsPanel, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(650, 650), new Vector2(.5f, .5f));
        AddHeading(optionsPanel.transform, "OPTION", -44);

        var volume = SliderControl(optionsPanel.transform, "마스터 볼륨", -135);
        volume.value = PlayerPrefs.GetFloat(VolumeKey, 1f);
        volume.onValueChanged.AddListener(SetVolume);

        var shake = ToggleControl(optionsPanel.transform, "화면 흔들림", -225);
        shake.isOn = PlayerPrefs.GetInt(ShakeKey, 1) != 0;
        shake.onValueChanged.AddListener(SetShake);

        var fullscreen = ToggleControl(optionsPanel.transform, "전체 화면", -295);
        fullscreen.isOn = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
        fullscreen.onValueChanged.AddListener(SetFullscreen);

        var controls = Label("Controls", optionsPanel.transform, "이동  A / D     점프  SPACE     공격  F\n대시  SHIFT     벽 관통  E     옵션  ESC", 20, TextAnchor.MiddleCenter);
        Place(controls.gameObject, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -395), new Vector2(560, 90), new Vector2(.5f, 1));
        ButtonControl(optionsPanel.transform, "계속", new Vector2(0, -530), () => SetOptions(false));
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
            hearts[i].color = i < current ? new Color(.93f, .16f, .25f) : new Color(.2f, .23f, .27f);
        }
    }

    void ShowDeath()
    {
        if (optionsPanel.activeSelf) SetOptions(false);
        deathPanel.SetActive(true);
        Pause();
    }

    void SetOptions(bool visible)
    {
        optionsPanel.SetActive(visible);
        if (visible) Pause(); else RestoreTime();
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToTitle()
    {
        Time.timeScale = 1f;
        paused = false;
        if (Application.CanStreamedLevelBeLoaded("TitleScene")) SceneManager.LoadScene("TitleScene");
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
