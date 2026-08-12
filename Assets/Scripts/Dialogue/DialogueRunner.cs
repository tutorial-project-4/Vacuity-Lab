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

    private static DialogueRunner instance;
    private Coroutine routine;
    private PlayerController lockedPlayer;
    private bool isTyping;

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

    public void StartDialogue(DialogueLine[] lines, PlayerController player)
    {
        if (lines == null || lines.Length == 0 || IsRunning)
        {
            return;
        }

        routine = StartCoroutine(DialogueRoutine(lines, player));
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
        lockedPlayer?.SetCutsceneLock(true);
        SetPromptVisible(false);
        SetDialogueVisible(true);
        yield return null;

        for (int index = 0; index < lines.Length; index++)
        {
            yield return TypeLine(lines[index]);
            yield return WaitForAdvance();
        }

        FinishDialogue();
    }

    private void FinishDialogue()
    {
        SetDialogueVisible(false);
        lockedPlayer?.SetCutsceneLock(false);
        lockedPlayer = null;
        routine = null;
        isTyping = false;
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

        FinishDialogue();
    }
}
