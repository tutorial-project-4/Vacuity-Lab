using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OptionsPrefabUI : MonoBehaviour
{
    const string VolumeKey = "ui.masterVolume";
    const string ShakeKey = "ui.screenShake";
    const string FullscreenKey = "ui.fullscreen";

    public static bool GodModeEnabled { get; private set; }

    PlayerHealth health;
    Button godModeButton;
    public void Initialize(PlayerHealth playerHealth, Action onClose, Action onExit)
    {
        health = playerHealth;
        GetComponent<Canvas>().sortingOrder = 100;

        Slider audio = Find<Slider>("AudioSlider");
        audio.value = PlayerPrefs.GetFloat(VolumeKey, 1f);
        audio.onValueChanged.AddListener(SetVolume);

        Bind("S_ON_BUTTON", () => SetShake(true));
        Bind("S_OFF_BUTTON", () => SetShake(false));
        Bind("F_ON_BUTTON", () => SetFullscreen(true));
        Bind("F_OFF_BUTTON", () => SetFullscreen(false));
        Bind("BACK_BUTTON ", onClose);
        Bind("EXIT_BUTTON", onExit);

        godModeButton = Find<Button>("God_mode");
        godModeButton.onClick.AddListener(ToggleGodMode);
        ApplyGodMode();
    }

    public void Show()
    {
        FindTransform("optionPannel").gameObject.SetActive(true);
        FindTransform("keyviewPANNEL").gameObject.SetActive(false);
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(Find<Slider>("AudioSlider").gameObject);
    }

    void ToggleGodMode()
    {
        GodModeEnabled = !GodModeEnabled;
        ApplyGodMode();
    }

    void ApplyGodMode()
    {
        if (health)
        {
            if (GodModeEnabled) health.AddInvincibleOverride(this);
            else health.RemoveInvincibleOverride(this);
        }

        godModeButton.targetGraphic.color = GodModeEnabled ? new Color(.35f, .85f, 1f) : Color.white;
        TMP_Text label = godModeButton.GetComponentInChildren<TMP_Text>(true);
        //if (label) label.text = GodModeEnabled ? "무적 ON" : "무적 OFF";
    }

    void OnDestroy()
    {
        if (health) health.RemoveInvincibleOverride(this);
    }

    void Bind(string name, Action action) => Find<Button>(name).onClick.AddListener(() => action());

    T Find<T>(string objectName) where T : Component
    {
        foreach (T component in GetComponentsInChildren<T>(true))
            if (component.name == objectName) return component;
        throw new MissingReferenceException($"[OptionsPrefabUI] Options.prefab에 '{objectName}' 오브젝트가 없습니다.");
    }

    Transform FindTransform(string objectName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == objectName) return child;
        throw new MissingReferenceException($"[OptionsPrefabUI] Options.prefab에 '{objectName}' 오브젝트가 없습니다.");
    }

    static void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
    }

    static void SetShake(bool value)
    {
        CinemachineScreenShake2D.Enabled = value;
        PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0);
    }

    static void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
    }
}
