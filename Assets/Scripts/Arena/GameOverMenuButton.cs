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
public class GameOverMenuButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private BossArenaRespawnController respawnController;
    [SerializeField] private GameOverMenuAction action;
    [SerializeField] private Text label;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    private void Awake()
    {
        CacheLabel();
        ApplyColor(normalColor);
    }

    public void OnPointerClick(PointerEventData eventData)
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

    private void ApplyColor(Color color)
    {
        CacheLabel();

        if (label != null)
        {
            label.color = color;
        }
    }
}
