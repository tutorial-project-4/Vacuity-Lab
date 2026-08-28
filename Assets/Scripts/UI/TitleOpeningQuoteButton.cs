using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TitleOpeningQuoteButton : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private OpeningQuoteSequence openingQuoteSequence;
    [SerializeField] private string nextSceneName = "semi-complete-arena";
    [SerializeField] private int startMode;
    [SerializeField] private bool replaceExistingNewGameListeners = true;

    const string StartModeKey = "game.startMode";
    bool isStarting;

    void Start()
    {
        if (newGameButton == null)
        {
            Debug.LogWarning("[TitleOpeningQuoteButton] New Game button is not assigned.", this);
            return;
        }

        if (replaceExistingNewGameListeners)
        {
            newGameButton.onClick.RemoveAllListeners();
        }

        newGameButton.onClick.AddListener(PlayOpeningQuote);
    }

    public void PlayOpeningQuote()
    {
        if (isStarting)
        {
            return;
        }

        if (openingQuoteSequence == null)
        {
            Debug.LogWarning("[TitleOpeningQuoteButton] Opening quote sequence is not assigned.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("[TitleOpeningQuoteButton] Next scene name is empty.", this);
            return;
        }

        isStarting = true;
        PlayerPrefs.SetInt(StartModeKey, startMode);
        PlayerPrefs.Save();
        newGameButton.interactable = false;
        openingQuoteSequence.Play(nextSceneName);
    }

    void OnValidate()
    {
        startMode = Mathf.Max(0, startMode);
    }
}
