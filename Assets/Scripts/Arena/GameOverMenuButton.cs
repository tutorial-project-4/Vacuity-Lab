using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum GameOverMenuAction
{
    Retry,
    ReloadScene
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Text))]
[RequireComponent(typeof(Button))]
public class GameOverMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private BossArenaRespawnController respawnController;
    [SerializeField] private GameOverMenuAction action;
    [SerializeField] private Text label;
    [SerializeField] private Button button;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    private void Awake()
    {
        CacheButton();
        CacheLabel();

        if (button != null)
        {
            button.onClick.AddListener(InvokeAction);
        }
        else
        {
            Debug.LogWarning("[GameOverMenuButton] Button component is missing.", this);
        }

        ApplyColor(normalColor);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(InvokeAction);
        }
    }

    private void InvokeAction()
    {
        if (respawnController == null)
        {
            Debug.LogWarning("[GameOverMenuButton] Respawn Controller가 연결되지 않았습니다.", this);
            return;
        }

        if (action == GameOverMenuAction.Retry)
        {
            respawnController.Retry();
        }
        else
        {
            respawnController.ReloadCurrentScene();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ApplyColor(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyColor(normalColor);
    }

    private void CacheLabel()
    {
        if (label == null)
        {
            label = GetComponent<Text>();
        }
    }

    private void CacheButton()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void ApplyColor(Color color)
    {
        CacheLabel();

        if (label != null)
        {
            label.color = color;
        }
    }

    private void OnValidate()
    {
        CacheButton();
        CacheLabel();
    }
}
