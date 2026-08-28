#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DialogueChoiceSetup
{
    private const string ScenePath = "Assets/Scenes/semi-complete-arena.unity";

    [MenuItem("Tools/Codex/Setup Dialogue Choice UI")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath);

        DialogueRunner runner = Object.FindFirstObjectByType<DialogueRunner>(FindObjectsInactive.Include);
        if (runner == null)
        {
            Debug.LogWarning("[Codex] DialogueRunner not found.");
            return;
        }

        Canvas canvas = runner.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            Debug.LogWarning("[Codex] Dialogue Canvas not found.");
            return;
        }

        RectTransform panel = canvas.transform.Find("Choice Panel") as RectTransform;
        if (panel == null)
        {
            GameObject panelObject = new GameObject("Choice Panel", typeof(RectTransform), typeof(CanvasGroup), typeof(VerticalLayoutGroup));
            panel = panelObject.GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 230f);
            panel.sizeDelta = new Vector2(560f, 150f);
        }

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 12f;

        CanvasGroup choiceGroup = panel.GetComponent<CanvasGroup>();
        choiceGroup.alpha = 0f;
        choiceGroup.interactable = false;
        choiceGroup.blocksRaycasts = false;

        Button[] buttons = new Button[2];
        Text[] texts = new Text[2];
        Text sourceText = canvas.transform.Find("Dialogue Panel/Dialogue Text")?.GetComponent<Text>();
        for (int i = 0; i < 2; i++)
        {
            CreateOrUpdateChoiceButton(panel, i, sourceText, out buttons[i], out texts[i]);
        }

        SerializedObject runnerObject = new SerializedObject(runner);
        runnerObject.FindProperty("choiceGroup").objectReferenceValue = choiceGroup;

        SerializedProperty buttonArray = runnerObject.FindProperty("choiceButtons");
        buttonArray.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
        {
            buttonArray.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        }

        SerializedProperty textArray = runnerObject.FindProperty("choiceTexts");
        textArray.arraySize = texts.Length;
        for (int i = 0; i < texts.Length; i++)
        {
            textArray.GetArrayElementAtIndex(i).objectReferenceValue = texts[i];
        }

        runnerObject.ApplyModifiedProperties();
        panel.gameObject.SetActive(false);

        EditorUtility.SetDirty(runner);
        EditorUtility.SetDirty(panel.gameObject);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Codex] Dialogue choice UI setup complete.");
    }

    private static void CreateOrUpdateChoiceButton(RectTransform parent, int index, Text sourceText, out Button button, out Text text)
    {
        string objectName = $"Choice Button {index}";
        Transform existing = parent.Find(objectName);
            GameObject buttonObject = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));

        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(560f, 60f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = 60f;
        layoutElement.preferredHeight = 60f;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.78f);

        button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Transform textTransform = buttonObject.transform.Find("Text");
        GameObject textObject = textTransform != null
            ? textTransform.gameObject
            : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));

        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 0f);
        textRect.offsetMax = new Vector2(-24f, 0f);

        text = textObject.GetComponent<Text>();
        text.text = index == 0 ? "1. 주사기를 맞는다" : "2. 맞지 않는다";
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.fontSize = 28;
        text.raycastTarget = false;

        if (sourceText != null)
        {
            text.font = sourceText.font;
            text.fontStyle = sourceText.fontStyle;
        }
    }
}
#endif
