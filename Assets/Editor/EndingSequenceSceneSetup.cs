using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EndingSequenceSceneSetup
{
    private const string ScenePath = "Assets/Scenes/semi-complete-arena.unity";

    [MenuItem("Tools/Ending/Setup Injection Ending")]
    public static void SetupInjectionEnding()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject boss2DoorTrigger = GameObject.Find("Boss2 Door Trigger");
        if (boss2DoorTrigger == null)
        {
            Debug.LogError("[EndingSequenceSceneSetup] Boss2 Door Trigger was not found.");
            return;
        }

        GameObject root = FindOrCreateRoot("EndingEventRoot");
        EndingChoiceState choiceState = GetOrAdd<EndingChoiceState>(root);
        EndingSequenceController sequenceController = GetOrAdd<EndingSequenceController>(root);
        AudioSource bgmSource = GetOrAdd<AudioSource>(root);
        bgmSource.playOnAwake = false;

        Canvas canvas = BuildEndingCanvas(root.transform);
        Transform screen = canvas.transform.Find("Ending Screen");
        CanvasGroup rootGroup = screen.GetComponent<CanvasGroup>();
        Image background = screen.GetComponent<Image>();
        Image slideImage = screen.Find("Ending Slide Image").GetComponent<Image>();
        Text speakerText = screen.Find("Ending Speaker Text").GetComponent<Text>();
        Text bodyText = screen.Find("Ending Body Text").GetComponent<Text>();

        ConfigureSequenceController(sequenceController, canvas, rootGroup, background, slideImage, speakerText, bodyText, bgmSource);

        SetEndingChoiceAction choiceAction = GetOrAdd<SetEndingChoiceAction>(boss2DoorTrigger);
        ConfigureChoiceAction(choiceAction, choiceState);

        EndingDoorTrigger endingTrigger = GetOrAdd<EndingDoorTrigger>(boss2DoorTrigger);
        ConfigureEndingTrigger(endingTrigger, choiceState, sequenceController);

        AutoDialogueTrigger autoDialogue = boss2DoorTrigger.GetComponent<AutoDialogueTrigger>();
        if (autoDialogue == null)
        {
            autoDialogue = boss2DoorTrigger.AddComponent<AutoDialogueTrigger>();
        }

        ConfigureBoss2DoorDialogue(autoDialogue, choiceAction);

        EditorUtility.SetDirty(boss2DoorTrigger);
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(sequenceController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[EndingSequenceSceneSetup] Injection ending setup complete.");
    }

    private static GameObject FindOrCreateRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            return existing;
        }

        return new GameObject(name);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static Canvas BuildEndingCanvas(Transform parent)
    {
        GameObject canvasObject = FindDirectChild(parent, "Ending Canvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Ending Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
        }

        SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        GameObject screenObject = FindDirectChild(canvasObject.transform, "Ending Screen");
        if (screenObject == null)
        {
            screenObject = new GameObject("Ending Screen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            screenObject.transform.SetParent(canvasObject.transform, false);
        }

        RectTransform screenRect = screenObject.GetComponent<RectTransform>();
        Stretch(screenRect);

        Image background = screenObject.GetComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = true;

        CanvasGroup group = screenObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        screenObject.SetActive(false);

        Image slideImage = CreateOrGetImage(screenObject.transform, "Ending Slide Image");
        RectTransform slideRect = slideImage.GetComponent<RectTransform>();
        slideRect.anchorMin = new Vector2(0.5f, 0.5f);
        slideRect.anchorMax = new Vector2(0.5f, 0.5f);
        slideRect.pivot = new Vector2(0.5f, 0.5f);
        slideRect.anchoredPosition = new Vector2(0f, 115f);
        slideRect.sizeDelta = new Vector2(1320f, 742f);
        slideImage.preserveAspect = true;
        slideImage.raycastTarget = false;
        slideImage.gameObject.SetActive(false);

        Text speakerText = CreateOrGetText(screenObject.transform, "Ending Speaker Text");
        RectTransform speakerRect = speakerText.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0.5f, 0f);
        speakerRect.anchorMax = new Vector2(0.5f, 0f);
        speakerRect.pivot = new Vector2(0.5f, 0f);
        speakerRect.anchoredPosition = new Vector2(0f, 262f);
        speakerRect.sizeDelta = new Vector2(1420f, 48f);
        speakerText.fontSize = 30;
        speakerText.alignment = TextAnchor.MiddleLeft;

        Text bodyText = CreateOrGetText(screenObject.transform, "Ending Body Text");
        RectTransform bodyRect = bodyText.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 0f);
        bodyRect.anchorMax = new Vector2(0.5f, 0f);
        bodyRect.pivot = new Vector2(0.5f, 0f);
        bodyRect.anchoredPosition = new Vector2(0f, 58f);
        bodyRect.sizeDelta = new Vector2(1420f, 210f);
        bodyText.fontSize = 28;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;

        return canvas;
    }

    private static Image CreateOrGetImage(Transform parent, string name)
    {
        GameObject existing = FindDirectChild(parent, name);
        if (existing == null)
        {
            existing = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            existing.transform.SetParent(parent, false);
        }

        return existing.GetComponent<Image>();
    }

    private static Text CreateOrGetText(Transform parent, string name)
    {
        GameObject existing = FindDirectChild(parent, name);
        if (existing == null)
        {
            existing = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            existing.transform.SetParent(parent, false);
        }

        Text text = existing.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        text.raycastTarget = false;
        text.supportRichText = true;
        return text;
    }

    private static GameObject FindDirectChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.gameObject : null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void ConfigureSequenceController(
        EndingSequenceController controller,
        Canvas canvas,
        CanvasGroup rootGroup,
        Image background,
        Image slideImage,
        Text speakerText,
        Text bodyText,
        AudioSource bgmSource)
    {
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("canvas").objectReferenceValue = canvas;
        serialized.FindProperty("rootGroup").objectReferenceValue = rootGroup;
        serialized.FindProperty("backgroundImage").objectReferenceValue = background;
        serialized.FindProperty("slideImage").objectReferenceValue = slideImage;
        serialized.FindProperty("speakerText").objectReferenceValue = speakerText;
        serialized.FindProperty("bodyText").objectReferenceValue = bodyText;
        serialized.FindProperty("bgmSource").objectReferenceValue = bgmSource;
        serialized.FindProperty("sfxSource").objectReferenceValue = bgmSource;
        serialized.FindProperty("titleSceneName").stringValue = "Title 1";
        serialized.FindProperty("fadeInDuration").floatValue = 1f;
        serialized.FindProperty("fadeOutDuration").floatValue = 0.8f;
        serialized.FindProperty("charactersPerSecond").floatValue = 35f;
        serialized.FindProperty("slideFadeDuration").floatValue = 0.35f;

        SerializedProperty slides = serialized.FindProperty("slides");
        slides.arraySize = 3;
        ConfigureSlide(slides.GetArrayElementAtIndex(0), "그림 1) 풀 숲", "Assets/Art/1. 풀숲.png",
            "우리는 무작정 풀을 헤치고 걸었다. 도로가 나올 때까지.\n한참 걷고 나서 마침내 도로를 발견한 우리는 간신히 풀만 가득한 이 산을 벗어났다.");
        ConfigureSlide(slides.GetArrayElementAtIndex(1), "그림 2) 모텔", "Assets/Art/2. 모텔.png",
            "근처 모텔에 도착한 후 대니는 이곳에 있으면 누군가 나를 데리러 올 것이라 일러주고 사라졌다. 아무것도 기억나지 않았지만 주사를 한 대 맞으니 기분이 괜찮아진다. 다행히 그 친절한 남자는 떠나면서 나에게 여러 개의 주사기를 주고 갔다.");
        ConfigureSlide(slides.GetArrayElementAtIndex(2), "그림 3) 텔레비전", "Assets/Art/3. 텔레비전.png",
            "얼마나 시간이 지났을까, 기억을 회복할 때마다 주사를 놓으니 시간의 흐름도 알 수 없어졌다. 갖고 있던 주사기는 모두 사용했다. 조금씩 기억이 돌아오는 감각이 불쾌하다. 오래된 모텔 TV에서 내 얼굴의 수배지가 방영되고 있다.\n[ Q 연구소 부소장 폴 맥그래스 ]\n연구원인 아내와 연구소장을 살해하고 연구소를 폭파, 현재는 도주 중... 아나운서가 심각한 목소리로 말한다. 도대체 영문을 모르겠는 소리 뿐이다. 기억이 조금씩 돌아오고 있다. 아주 불쾌하고 역겨운 기분이다. 주사기, 주사기가 필요하다.\n[end1 주사기가 필요해]");

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSlide(SerializedProperty slide, string title, string imagePath, string text)
    {
        slide.FindPropertyRelative("title").stringValue = title;
        slide.FindPropertyRelative("image").objectReferenceValue = LoadSprite(imagePath);
        slide.FindPropertyRelative("text").stringValue = text;
    }

    private static Sprite LoadSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        return assets.OfType<Sprite>().FirstOrDefault();
    }

    private static void ConfigureChoiceAction(SetEndingChoiceAction action, EndingChoiceState choiceState)
    {
        SerializedObject serialized = new SerializedObject(action);
        serialized.FindProperty("choiceState").objectReferenceValue = choiceState;
        serialized.FindProperty("choice").enumValueIndex = (int)EndingChoice.AcceptedInjection;
        serialized.FindProperty("healPlayerToFull").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureEndingTrigger(EndingDoorTrigger trigger, EndingChoiceState choiceState, EndingSequenceController sequenceController)
    {
        SerializedObject serialized = new SerializedObject(trigger);
        serialized.FindProperty("choiceState").objectReferenceValue = choiceState;
        serialized.FindProperty("sequenceController").objectReferenceValue = sequenceController;
        serialized.FindProperty("requireAcceptedInjection").boolValue = true;
        serialized.FindProperty("triggerOnce").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBoss2DoorDialogue(AutoDialogueTrigger trigger, SetEndingChoiceAction choiceAction)
    {
        SerializedObject serialized = new SerializedObject(trigger);
        SerializedProperty lines = serialized.FindProperty("lines");
        lines.arraySize = 1;

        SerializedProperty promptLine = lines.GetArrayElementAtIndex(0);
        SetDialogueLine(promptLine, string.Empty, "주사기를 어떻게 할까?");

        SerializedProperty choices = promptLine.FindPropertyRelative("choices");
        choices.arraySize = 2;

        SerializedProperty acceptChoice = choices.GetArrayElementAtIndex(0);
        acceptChoice.FindPropertyRelative("text").stringValue = "주사기를 맞는다";
        SerializedProperty acceptActions = acceptChoice.FindPropertyRelative("actionsOnChoose");
        acceptActions.arraySize = 1;
        acceptActions.GetArrayElementAtIndex(0).objectReferenceValue = choiceAction;
        SerializedProperty acceptLines = acceptChoice.FindPropertyRelative("nextLines");
        acceptLines.arraySize = 4;
        SetDialogueLine(acceptLines.GetArrayElementAtIndex(0), "폴", "알았어.");
        SetDialogueLine(acceptLines.GetArrayElementAtIndex(1), string.Empty, "마음이 편안해진다. 조금 나른한 것 같기도 하다. 몸 상태가 회복되었다.");
        SetDialogueLine(acceptLines.GetArrayElementAtIndex(2), "대니", "이제 나갈까?");
        SetDialogueLine(acceptLines.GetArrayElementAtIndex(3), "폴", "그래.");

        SerializedProperty rejectChoice = choices.GetArrayElementAtIndex(1);
        rejectChoice.FindPropertyRelative("text").stringValue = "맞지 않는다";
        rejectChoice.FindPropertyRelative("actionsOnChoose").arraySize = 0;
        rejectChoice.FindPropertyRelative("nextLines").arraySize = 0;

        serialized.FindProperty("triggerOnce").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetDialogueLine(SerializedProperty line, string speaker, string text)
    {
        line.FindPropertyRelative("speaker").stringValue = speaker;
        line.FindPropertyRelative("text").stringValue = text;
        line.FindPropertyRelative("image").objectReferenceValue = null;
        line.FindPropertyRelative("hideDialogueWhileImage").boolValue = false;
        line.FindPropertyRelative("choices").arraySize = 0;
    }
}
