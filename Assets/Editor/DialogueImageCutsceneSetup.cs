#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DialogueImageCutsceneSetup
{
    private const string ScenePath = "Assets/Scenes/semi-complete-arena.unity";

    [MenuItem("Tools/Codex/Setup Dialogue Image Cutscenes")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath);

        DialogueRunner runner = Object.FindFirstObjectByType<DialogueRunner>(FindObjectsInactive.Include);
        if (runner == null)
        {
            Debug.LogWarning("[Codex] DialogueRunner not found.");
            return;
        }

        SetupImageOverlay(runner);
        SetupWalletRewardDialogue();

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Codex] Dialogue image cutscene setup complete.");
    }

    private static void SetupImageOverlay(DialogueRunner runner)
    {
        Canvas canvas = runner.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            Debug.LogWarning("[Codex] Dialogue Canvas not found.");
            return;
        }

        RectTransform overlay = canvas.transform.Find("Dialogue Image Overlay") as RectTransform;
        if (overlay == null)
        {
            GameObject overlayObject = new GameObject("Dialogue Image Overlay", typeof(RectTransform), typeof(CanvasGroup));
            overlay = overlayObject.GetComponent<RectTransform>();
            overlay.SetParent(canvas.transform, false);
            Stretch(overlay);
        }

        CanvasGroup overlayGroup = overlay.GetComponent<CanvasGroup>();
        if (overlayGroup == null)
        {
            overlayGroup = overlay.gameObject.AddComponent<CanvasGroup>();
        }

        Image background = overlay.Find("Background")?.GetComponent<Image>();
        if (background == null)
        {
            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(overlay, false);
            RectTransform backgroundTransform = backgroundObject.GetComponent<RectTransform>();
            Stretch(backgroundTransform);
            background = backgroundObject.GetComponent<Image>();
        }

        background.color = new Color(0f, 0f, 0f, 0.82f);
        background.raycastTarget = true;

        Image image = overlay.Find("Image")?.GetComponent<Image>();
        if (image == null)
        {
            GameObject imageObject = new GameObject("Image", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(overlay, false);
            RectTransform imageTransform = imageObject.GetComponent<RectTransform>();
            imageTransform.anchorMin = new Vector2(0.5f, 0.5f);
            imageTransform.anchorMax = new Vector2(0.5f, 0.5f);
            imageTransform.pivot = new Vector2(0.5f, 0.5f);
            imageTransform.anchoredPosition = Vector2.zero;
            imageTransform.sizeDelta = new Vector2(1000f, 620f);
            image = imageObject.GetComponent<Image>();
        }

        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;

        overlayGroup.alpha = 0f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;
        overlay.gameObject.SetActive(false);

        SerializedObject runnerObject = new SerializedObject(runner);
        runnerObject.FindProperty("imageOverlayGroup").objectReferenceValue = overlayGroup;
        runnerObject.FindProperty("dialogueImage").objectReferenceValue = image;
        runnerObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(runner);
    }

    private static void SetupWalletRewardDialogue()
    {
        GameObject wallet = GameObject.Find("Wallet");
        if (wallet == null)
        {
            Debug.LogWarning("[Codex] Wallet object not found.");
            return;
        }

        DialogueInteractable interactable = wallet.GetComponent<DialogueInteractable>();
        if (interactable == null)
        {
            Debug.LogWarning("[Codex] DialogueInteractable not found on Wallet.");
            return;
        }

        Sprite walletPhoto = LoadSprite("Assets/Art/카드지갑 사진.png");
        Sprite doubleJump = LoadSprite("Assets/Art/doubleJump.png");
        Sprite phaseDash = LoadSprite("Assets/Art/벽뚫대시.png");

        List<DialogueLine> lines = new List<DialogueLine>();

        lines.Add(Line("나레이션", "지갑이다. 안에 카드 키가 있을 것이다."));
        lines.Add(Line("폴", "드디어 찾았다."));
        lines.Add(Line("나레이션", "지갑 속에 낯익은 남자와 여자의 사진이 있다. 둘은 남매처럼 보인다."));
        lines.Add(ImageLine(walletPhoto));
        lines.Add(Line("나레이션", "어딘가 그리운 기분이 든다."));
        lines.Add(Line("돌연변이 연구원", "ㅍ...ㅗㄹ..."));
        lines.Add(Line("나레이션", "괴물이 무언가를 더 말하려고 했지만 얼마 가지 못해 숨이 끊어졌다."));
        lines.Add(Line("폴", "이 기분은 도대체 뭐지? 뭔가 중요한 것을 놓치고 있는 것 같아."));
        lines.Add(Line("폴", "큭..! 머리가, 머리가 너무 아파!"));
        lines.Add(ImageLine(doubleJump));
        lines.Add(ImageLine(phaseDash));
        lines.Add(Line("폴", "연구원의 이름은 제이콥이었어. 제이콥. 제이콥... 나는 그와 꽤 친분이 있는 사이였던 거 같은데, 젠장 도무지 뚜렷하게 기억이 나지 않아. 조금 더 단서가 있었다면...!"));
        lines.Add(Line("폴", "후, 우선 대니에게 돌아가 봐야겠어."));

        SerializedObject interactableObject = new SerializedObject(interactable);
        WriteLines(interactableObject.FindProperty("lines"), lines);
        interactableObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(interactable);
    }

    private static DialogueLine Line(string speaker, string text)
    {
        return new DialogueLine
        {
            speaker = speaker,
            text = text,
            hideDialogueWhileImage = false
        };
    }

    private static DialogueLine ImageLine(Sprite image)
    {
        return new DialogueLine
        {
            speaker = string.Empty,
            text = string.Empty,
            image = image,
            hideDialogueWhileImage = true
        };
    }

    private static List<DialogueLine> ReadLines(DialogueInteractable interactable)
    {
        SerializedObject interactableObject = new SerializedObject(interactable);
        SerializedProperty linesProperty = interactableObject.FindProperty("lines");
        List<DialogueLine> lines = new List<DialogueLine>();

        for (int i = 0; i < linesProperty.arraySize; i++)
        {
            SerializedProperty lineProperty = linesProperty.GetArrayElementAtIndex(i);
            lines.Add(new DialogueLine
            {
                speaker = lineProperty.FindPropertyRelative("speaker").stringValue,
                text = lineProperty.FindPropertyRelative("text").stringValue,
                image = lineProperty.FindPropertyRelative("image").objectReferenceValue as Sprite,
                hideDialogueWhileImage = lineProperty.FindPropertyRelative("hideDialogueWhileImage").boolValue
            });
        }

        return lines;
    }

    private static void WriteLines(SerializedProperty linesProperty, IReadOnlyList<DialogueLine> lines)
    {
        linesProperty.arraySize = lines.Count;
        for (int i = 0; i < lines.Count; i++)
        {
            SerializedProperty lineProperty = linesProperty.GetArrayElementAtIndex(i);
            lineProperty.FindPropertyRelative("speaker").stringValue = lines[i].speaker;
            lineProperty.FindPropertyRelative("text").stringValue = lines[i].text;
            lineProperty.FindPropertyRelative("image").objectReferenceValue = lines[i].image;
            lineProperty.FindPropertyRelative("hideDialogueWhileImage").boolValue = lines[i].hideDialogueWhileImage;
        }
    }

    private static void UpdateClearSegmentRange(SerializedProperty segmentsProperty, int start, int end)
    {
        if (segmentsProperty == null)
        {
            return;
        }

        for (int i = 0; i < segmentsProperty.arraySize; i++)
        {
            SerializedProperty segment = segmentsProperty.GetArrayElementAtIndex(i);
            SerializedProperty condition = segment.FindPropertyRelative("condition");
            if (condition != null && condition.enumValueIndex == 2)
            {
                segment.FindPropertyRelative("startIndex").intValue = start;
                segment.FindPropertyRelative("endIndexInclusive").intValue = end;
                return;
            }
        }
    }

    private static Sprite LoadSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        Sprite sprite = assets.OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
        {
            Debug.LogWarning($"[Codex] Sprite not found at {path}");
        }

        return sprite;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
#endif
