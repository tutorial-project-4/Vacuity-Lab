using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueRunner : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Key[] advanceKeys = { Key.F, Key.Space, Key.Enter };

    [Header("Typewriter")]
    [SerializeField] private float charactersPerSecond = 35f;

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Text promptText;
    [SerializeField] private CanvasGroup imageOverlayGroup;
    [SerializeField] private Image dialogueImage;
    [SerializeField] private CanvasGroup choiceGroup;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private Text[] choiceTexts;

    private static DialogueRunner instance;
    private Coroutine routine;
    private PlayerController lockedPlayer;
    private bool isTyping;
    private int selectedChoiceIndex = -1;

    public static DialogueRunner Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<DialogueRunner>();
#else
                instance = FindObjectOfType<DialogueRunner>();
#endif
            }

            return instance;
        }
    }

    public bool IsRunning => routine != null;
    public bool LastDialogueCompleted { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        CacheUiReferences();
        SetDialogueVisible(false);
        SetPromptVisible(false);
        SetImageOverlayVisible(false);
        SetChoiceVisible(false);
    }

    public void ShowPrompt(bool visible, string label)
    {
        CacheUiReferences();

        if (promptText == null || IsRunning)
        {
            return;
        }

        promptText.text = label;
        SetPromptVisible(visible);
    }

    public bool StartDialogue(DialogueLine[] lines, PlayerController player)
    {
        if (lines == null || lines.Length == 0 || IsRunning)
        {
            return false;
        }

        LastDialogueCompleted = false;
        routine = StartCoroutine(DialogueRoutine(lines, player));
        return true;
    }

    private IEnumerator DialogueRoutine(DialogueLine[] lines, PlayerController player)
    {
        CacheUiReferences();
        if (dialogueGroup == null || speakerText == null || dialogueText == null)
        {
            Debug.LogWarning("[DialogueRunner] Dialogue UI references are missing.", this);
            routine = null;
            yield break;
        }

        lockedPlayer = player;
        lockedPlayer?.SetDialogueInputLock(true);
        SetPromptVisible(false);
        SetDialogueVisible(true);
        yield return null;

        yield return PlayLines(lines);

        FinishDialogue(true);
    }

    private IEnumerator PlayLines(DialogueLine[] lines)
    {
        for (int index = 0; index < lines.Length; index++)
        {
            yield return TypeLine(lines[index]);
            if (lines[index].image != null)
            {
                yield return ShowImageLine(lines[index]);
                if (string.IsNullOrWhiteSpace(lines[index].text))
                {
                    continue;
                }
            }

            if (HasChoices(lines[index]))
            {
                yield return ShowChoices(lines[index].choices);
                continue;
            }

            yield return WaitForAdvance();
        }
    }

    private void FinishDialogue(bool completed)
    {
        SetDialogueVisible(false);
        SetImageOverlayVisible(false);
        SetChoiceVisible(false);
        lockedPlayer?.SetDialogueInputLock(false);
        lockedPlayer = null;
        routine = null;
        isTyping = false;
        LastDialogueCompleted = completed;
    }

    private IEnumerator TypeLine(DialogueLine line)
    {
        speakerText.gameObject.SetActive(!string.IsNullOrWhiteSpace(line.speaker));
        speakerText.text = line.speaker;
        dialogueText.text = string.Empty;

        isTyping = true;

        string fullText = line.text ?? string.Empty;
        float interval = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

        for (int i = 0; i < fullText.Length; i++)
        {
            if (WasAdvancePressed())
            {
                break;
            }

            dialogueText.text += fullText[i];

            if (interval > 0f)
            {
                float elapsed = 0f;
                while (elapsed < interval)
                {
                    if (WasAdvancePressed())
                    {
                        dialogueText.text = fullText;
                        isTyping = false;
                        yield break;
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return null;
            }
        }

        dialogueText.text = fullText;
        isTyping = false;
    }

    private IEnumerator WaitForAdvance()
    {
        yield return null;

        while (!WasAdvancePressed())
        {
            yield return null;
        }
    }

    private IEnumerator ShowImageLine(DialogueLine line)
    {
        if (imageOverlayGroup == null || dialogueImage == null)
        {
            yield break;
        }

        bool dialogueWasHidden = line.hideDialogueWhileImage;
        if (dialogueWasHidden)
        {
            SetDialogueVisible(false);
        }

        dialogueImage.sprite = line.image;
        dialogueImage.preserveAspect = true;
        SetImageOverlayVisible(true);

        yield return WaitForAdvance();

        SetImageOverlayVisible(false);
        if (dialogueWasHidden)
        {
            SetDialogueVisible(true);
        }
    }

    private IEnumerator ShowChoices(DialogueChoice[] choices)
    {
        if (choiceGroup == null || choiceButtons == null || choiceTexts == null)
        {
            Debug.LogWarning("[DialogueRunner] Choice UI references are missing.", this);
            yield return WaitForAdvance();
            yield break;
        }

        selectedChoiceIndex = -1;
        int visibleCount = Mathf.Min(choices.Length, choiceButtons.Length, choiceTexts.Length);
        if (visibleCount <= 0)
        {
            Debug.LogWarning("[DialogueRunner] Choice UI has no usable buttons.", this);
            yield return WaitForAdvance();
            yield break;
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool visible = i < visibleCount;
            choiceButtons[i].gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            int choiceIndex = i;
            choiceTexts[i].text = $"{i + 1}. {choices[i].text}";
            EnsureChoiceButtonVisible(choiceButtons[i], choiceTexts[i]);
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => selectedChoiceIndex = choiceIndex);
        }

        SetChoiceVisible(true);
        yield return null;

        while (selectedChoiceIndex < 0)
        {
            int keyboardChoice = GetKeyboardChoiceIndex(visibleCount);
            if (keyboardChoice >= 0)
            {
                selectedChoiceIndex = keyboardChoice;
            }

            yield return null;
        }

        SetChoiceVisible(false);
        DialogueChoice selectedChoice = choices[selectedChoiceIndex];
        RunChoiceActions(selectedChoice.actionsOnChoose);
        if (selectedChoice.nextLines != null && selectedChoice.nextLines.Length > 0)
        {
            yield return PlayLines(selectedChoice.nextLines);
        }
    }

    private bool WasAdvancePressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        for (int i = 0; i < advanceKeys.Length; i++)
        {
            if (keyboard[advanceKeys[i]].wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private int GetKeyboardChoiceIndex(int visibleCount)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return -1;
        }

        if (visibleCount > 0 && keyboard.digit1Key.wasPressedThisFrame)
        {
            return 0;
        }

        if (visibleCount > 1 && keyboard.digit2Key.wasPressedThisFrame)
        {
            return 1;
        }

        if (visibleCount > 2 && keyboard.digit3Key.wasPressedThisFrame)
        {
            return 2;
        }

        return -1;
    }

    private static bool HasChoices(DialogueLine line)
    {
        return line.choices != null && line.choices.Length > 0;
    }

    private static void RunChoiceActions(DialogueInteractionAction[] actions)
    {
        if (actions == null)
        {
            return;
        }

        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i] != null)
            {
                actions[i].Run();
            }
        }
    }

    private static void EnsureChoiceButtonVisible(Button button, Text label)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null && rect.sizeDelta.y < 1f)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x > 1f ? rect.sizeDelta.x : 560f, 60f);
        }

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.minHeight = 60f;
        layout.preferredHeight = 60f;

        Image image = button.targetGraphic as Image;
        if (image != null && image.color.a <= 0f)
        {
            image.color = new Color(0f, 0f, 0f, 0.78f);
        }

        if (label != null)
        {
            label.enabled = true;
            label.color = Color.white;
        }
    }

    private void SetDialogueVisible(bool visible)
    {
        if (dialogueGroup == null)
        {
            return;
        }

        dialogueGroup.alpha = visible ? 1f : 0f;
        dialogueGroup.interactable = visible;
        dialogueGroup.blocksRaycasts = visible;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(visible);
        }
        else if (promptText != null)
        {
            promptText.gameObject.SetActive(visible);
        }
    }

    private void SetImageOverlayVisible(bool visible)
    {
        if (imageOverlayGroup == null)
        {
            return;
        }

        imageOverlayGroup.alpha = visible ? 1f : 0f;
        imageOverlayGroup.interactable = visible;
        imageOverlayGroup.blocksRaycasts = visible;

        if (imageOverlayGroup.gameObject.activeSelf != visible)
        {
            imageOverlayGroup.gameObject.SetActive(visible);
        }
    }

    private void SetChoiceVisible(bool visible)
    {
        if (choiceGroup == null)
        {
            return;
        }

        choiceGroup.alpha = visible ? 1f : 0f;
        choiceGroup.interactable = visible;
        choiceGroup.blocksRaycasts = visible;

        if (choiceGroup.gameObject.activeSelf != visible)
        {
            choiceGroup.gameObject.SetActive(visible);
        }
    }

    private void CacheUiReferences()
    {
        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>(true);
        }

        if (dialogueGroup == null && canvas != null)
        {
            Transform panel = canvas.transform.Find("Dialogue Panel");
            if (panel != null)
            {
                dialogueGroup = panel.GetComponent<CanvasGroup>();
            }
        }

        if (canvas == null)
        {
            return;
        }

        speakerText ??= canvas.transform.Find("Dialogue Panel/Speaker Text")?.GetComponent<Text>();
        dialogueText ??= canvas.transform.Find("Dialogue Panel/Dialogue Text")?.GetComponent<Text>();

        if (imageOverlayGroup == null)
        {
            imageOverlayGroup = canvas.transform.Find("Dialogue Image Overlay")?.GetComponent<CanvasGroup>();
        }

        if (dialogueImage == null && imageOverlayGroup != null)
        {
            dialogueImage = imageOverlayGroup.transform.Find("Image")?.GetComponent<Image>();
        }

        if (choiceGroup == null)
        {
            choiceGroup = canvas.transform.Find("Choice Panel")?.GetComponent<CanvasGroup>();
        }

        if (choiceGroup != null && (choiceButtons == null || choiceButtons.Length == 0))
        {
            choiceButtons = choiceGroup.GetComponentsInChildren<Button>(true);
        }

        if (choiceGroup != null && (choiceTexts == null || choiceTexts.Length == 0))
        {
            choiceTexts = choiceGroup.GetComponentsInChildren<Text>(true);
        }

        if (promptRoot == null)
        {
            promptRoot = canvas.transform.Find("Interaction Prompt")?.gameObject;
        }

        if (promptText == null && promptRoot != null)
        {
            promptText = promptRoot.GetComponent<Text>();
        }
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        FinishDialogue(false);
    }
}
