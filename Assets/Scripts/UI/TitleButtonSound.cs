using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class TitleButtonSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private TitleAudioController audioController;
    [SerializeField] private Button button;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private bool fadeOutTitleBgmOnClick;
    [SerializeField] private float titleBgmFadeOutDuration = 0.6f;

    private void Awake()
    {
        CacheReferences();
    }

    private IEnumerator Start()
    {
        yield return null;

        CacheReferences();
        if (button != null)
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClick);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHover();
    }

    public void OnSelect(BaseEventData eventData)
    {
        PlayHover();
    }

    private void PlayHover()
    {
        GetAudioController()?.PlayHover(hoverClip);
    }

    private void PlayClick()
    {
        TitleAudioController controller = GetAudioController();
        if (controller == null)
        {
            return;
        }

        controller.PlayClick(clickClip);
        if (fadeOutTitleBgmOnClick)
        {
            controller.FadeOutTitleBgm(titleBgmFadeOutDuration);
        }
    }

    private TitleAudioController GetAudioController()
    {
        if (audioController != null)
        {
            return audioController;
        }

#if UNITY_2023_1_OR_NEWER
        audioController = FindFirstObjectByType<TitleAudioController>();
#else
        audioController = FindObjectOfType<TitleAudioController>();
#endif
        return audioController;
    }

    private void CacheReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        GetAudioController();
    }

    private void OnValidate()
    {
        titleBgmFadeOutDuration = Mathf.Max(0f, titleBgmFadeOutDuration);
        CacheReferences();
    }
}
